using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EFApp.Domain.Entities;
using EFApp.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace EFApp.WebApi.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<EFApp.WebApi.Program>>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly WebApplicationFactory<EFApp.WebApi.Program> _factory;

        public IntegrationTests(WebApplicationFactory<EFApp.WebApi.Program> factory, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _factory = factory.WithWebHostBuilder(builder =>
                builder.UseEnvironment("IntegrationTests")
            );

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Doctors.AddRange(
                new Doctor { FirstName = "Anna",   LastName = "Kowalska",   Email = "anna@przychodnia.pl" },
                new Doctor { FirstName = "Michał", LastName = "Wiśniewski", Email = "michal@przychodnia.pl" }
            );
            db.Medicaments.AddRange(
                new Medicament { Name = "Paracetamol", Description = "Przeciwbólowy",   Type = "Tabletka" },
                new Medicament { Name = "Ibuprofen",   Description = "Przeciwzapalny",  Type = "Tabletka" }
            );
            db.SaveChanges();
        }

        private HttpClient CreateClient() => _factory.CreateClient();

        [Fact]
        public async void EndToEnd_CreateAndGet()
        {
            var client = CreateClient();

            var presDto = new
            {
                patient = new { firstName = "Jan", lastName = "Kowalski", birthdate = "1990-01-01" },
                idDoctor    = 1,
                date        = "2025-05-22T00:00:00",
                dueDate     = "2025-06-22T00:00:00",
                medicaments = new[]
                {
                    new { idMedicament = 1, dose = 2, details = "2xdziennie" }
                }
            };
            var postResp = await client.PostAsJsonAsync("/api/prescriptions", presDto);
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);


            var err = await postResp.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);


            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(1, db.Patients.Count());
            }

            var getResp = await client.GetAsync("/api/patients/1");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var json = await getResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, json.GetProperty("patient").GetProperty("idPatient").GetInt32());
            Assert.Single(json.GetProperty("prescriptions").EnumerateArray());
        }

        [Fact]
        public async void CreatePrescription_NonExistingMedicament_Returns404()
        {
            var client = CreateClient();

            var presDto = new
            {
                patient = new { firstName = "Ewa", lastName = "Nowak", birthdate = "1985-05-05" },
                idDoctor    = 1,
                date        = "2025-05-22T00:00:00",
                dueDate     = "2025-06-22T00:00:00",
                medicaments = new[]
                {
                    new { idMedicament = 999, dose = 1, details = "x" }
                }
            };
            var resp = await client.PostAsJsonAsync("/api/prescriptions", presDto);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async void CreatePrescription_TooManyMedicaments_Returns400()
        {
            var client = CreateClient();

            var many = Enumerable.Range(1, 11)
                .Select(i => new { idMedicament = 1, dose = 1, details = $"d{i}" })
                .ToArray();
            var presDto = new
            {
                patient = new { firstName = "Ola", lastName = "Kot", birthdate = "1992-02-02" },
                idDoctor    = 1,
                date        = "2025-05-22T00:00:00",
                dueDate     = "2025-06-22T00:00:00",
                medicaments = many
            };
            var resp = await client.PostAsJsonAsync("/api/prescriptions", presDto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async void CreatePrescription_DueDateBeforeDate_Returns400()
        {
            var client = CreateClient();

            var presDto = new
            {
                patient = new { firstName = "Piotr", lastName = "Zielony", birthdate = "1970-07-07" },
                idDoctor    = 1,
                date        = "2025-06-22T00:00:00",
                dueDate     = "2025-05-22T00:00:00",
                medicaments = new[]
                {
                    new { idMedicament = 1, dose = 1, details = "x" }
                }
            };
            var resp = await client.PostAsJsonAsync("/api/prescriptions", presDto);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async void GetPatientDetails_ReturnsFullData_SortedByDueDate()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var patient = new Patient
                {
                    FirstName = "Jan",
                    LastName  = "Kowalski",
                    Birthdate = new DateTime(1990, 1, 1)
                };
                db.Patients.Add(patient);
                db.SaveChanges();

                var earlier = new Prescription
                {
                    IdPatient   = patient.IdPatient,
                    IdDoctor    = 1,
                    Date        = new DateTime(2025, 4, 1),
                    DueDate     = new DateTime(2025, 5, 1),
                    RowVersion  = new byte[8],
                    PrescriptionMedicaments = new[]
                    {
                        new PrescriptionMedicament
                        {
                            IdMedicament = 1,
                            Dose         = 3,
                            Details      = "Some desc..."
                        }
                    }.ToList()
                };

                var later = new Prescription
                {
                    IdPatient   = patient.IdPatient,
                    IdDoctor    = 2,
                    Date        = new DateTime(2025, 5, 1),
                    DueDate     = new DateTime(2025, 6, 1),
                    RowVersion  = new byte[8],
                    PrescriptionMedicaments = new[]
                    {
                        new PrescriptionMedicament
                        {
                            IdMedicament = 2,
                            Dose         = 10,
                            Details      = "AAA"
                        }
                    }.ToList()
                };

                db.Prescriptions.AddRange(earlier, later);
                db.SaveChanges();
            }

            var client = CreateClient();

            var response = await client.GetAsync("/api/patients/1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var p = json.GetProperty("patient");
            Assert.Equal(1, p.GetProperty("idPatient").GetInt32());
            Assert.Equal("Jan", p.GetProperty("firstName").GetString());
            Assert.Equal("Kowalski", p.GetProperty("lastName").GetString());

            var pres = json.GetProperty("prescriptions").EnumerateArray().ToList();
            Assert.Equal(2, pres.Count);


            var first = pres[0];
            Assert.Equal("2025-05-01T00:00:00", first.GetProperty("dueDate").GetString());
            var med1 = first.GetProperty("medicaments")[0];
            Assert.Equal(1, med1.GetProperty("idMedicament").GetInt32());
            Assert.Equal(3, med1.GetProperty("dose").GetInt32());
            Assert.Equal("Some desc...", med1.GetProperty("details").GetString());
            var doc1 = first.GetProperty("doctor");
            Assert.Equal(1, doc1.GetProperty("idDoctor").GetInt32());
            Assert.Equal("Anna", doc1.GetProperty("firstName").GetString());

            var second = pres[1];
            Assert.Equal("2025-06-01T00:00:00", second.GetProperty("dueDate").GetString());
            var med2 = second.GetProperty("medicaments")[0];
            Assert.Equal(2,  med2.GetProperty("idMedicament").GetInt32());
            Assert.Equal(10, med2.GetProperty("dose").GetInt32());
            Assert.Equal("AAA", med2.GetProperty("details").GetString());
            var doc2 = second.GetProperty("doctor");
            Assert.Equal(2,          doc2.GetProperty("idDoctor").GetInt32());
            Assert.Equal("Michał",   doc2.GetProperty("firstName").GetString());
        }
    }
}
