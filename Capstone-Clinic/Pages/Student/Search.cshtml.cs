using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.Student;

public class SearchModel : PageModel
{
    private readonly AppDbContext _context;

    public SearchModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public string SearchStudentNumber { get; set; } = "";

    public Models.Student? Student { get; set; }
    public Models.StudentAccount? StudentAccount { get; set; }

    public bool StudentNotFound { get; set; }

    public bool HasMedicalRecord { get; set; }

    public int MedicalRecordId { get; set; }
    public List<Models.MedicalRequirement> MedicalRequirements { get; set; } = new();
    public class PatientSearchResult
    {
        public string PatientType { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Identifier { get; set; } = "";
        public int? StudentId { get; set; }
        public int? ClinicPatientId { get; set; }
        public int? MedicalRecordId { get; set; }
    }
    public List<PatientSearchResult> PatientResults { get; set; } = new();
    public Dictionary<int, List<Models.DentalRecord>> DentalHistory { get; set; } = new();
    public Dictionary<int, int> StudentClinicPatientIds { get; set; } = new();

    public async Task OnGetAsync(string? studentNumber)
    {
        if (!string.IsNullOrWhiteSpace(studentNumber))
        {
            SearchStudentNumber = studentNumber;

            await LoadStudentAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchStudentNumber))
        {
            ModelState.AddModelError(
                nameof(SearchStudentNumber),
                "Please enter a Student Number or student name.");

            return Page();
        }

        SearchStudentNumber = SearchStudentNumber.Trim();
        Console.WriteLine(
    $"===== PATIENT SEARCH: [{SearchStudentNumber}] ====="
);

        await LoadStudentAsync();

        return Page();
    }
    public async Task<IActionResult> OnPostUpdateRequirementAsync(
    int medicalRequirementId,
    string status)
    {
        if (status != "Not Submitted" && status != "Complete")
        {
            return BadRequest();
        }

        var requirement = await _context.MedicalRequirements
            .FirstOrDefaultAsync(r =>
                r.MedicalRequirementId == medicalRequirementId);

        if (requirement == null)
        {
            return NotFound();
        }

        requirement.Status = status;

        await _context.SaveChangesAsync();

        var studentNumber = await _context.Students
    .Where(s => s.StudentId == requirement.StudentId)
    .Select(s => s.StudentNumber)
    .FirstOrDefaultAsync();

        if (studentNumber == null)
        {
            return NotFound();
        }

        return RedirectToPage(new
        {
            studentNumber
        });
    }

    private async Task LoadStudentAsync()
    {
        PatientResults = new List<PatientSearchResult>();

        var search = SearchStudentNumber.Trim().ToLower();

        var students = await _context.Students
            .Where(s =>
                s.StudentNumber.ToLower().Contains(search) ||
                s.FullName.ToLower().Contains(search))
            .Select(s => new PatientSearchResult
            {
                PatientType = "Student",
                FullName = s.FullName,
                Identifier = s.StudentNumber,
                StudentId = s.StudentId
            })
            .ToListAsync();

        Console.WriteLine("===== COMMUNITY PATIENTS =====");

        var allCommunityPatients = await _context.ClinicPatients
            .Where(p => p.StudentId == null)
            .Select(p => new
            {
                p.ClinicPatientId,
                p.PatientType,
                p.FullName,
                p.Identifier,
                p.StudentId
            })
            .ToListAsync();

        foreach (var patient in allCommunityPatients)
        {
            Console.WriteLine(
                $"ID={patient.ClinicPatientId} | " +
                $"Type={patient.PatientType} | " +
                $"Name={patient.FullName} | " +
                $"Identifier={patient.Identifier} | " +
                $"StudentId={patient.StudentId}"
            );
        }

        Console.WriteLine("==============================");
        var communityPatients = await _context.ClinicPatients
    .Where(p =>
        p.StudentId == null &&
        (
            (p.FullName != null &&
             p.FullName.ToLower().Contains(search)) ||
            (p.Identifier != null &&
             p.Identifier.ToLower().Contains(search))
        ))
    .Select(p => new PatientSearchResult
    {
        PatientType = p.PatientType,
        FullName = p.FullName ?? "",
        Identifier = p.Identifier ?? "",
        ClinicPatientId = p.ClinicPatientId,
        MedicalRecordId = _context.MedicalRecords
            .Where(m => m.ClinicPatientId == p.ClinicPatientId)
            .OrderByDescending(m => m.MedicalRecordId)
            .Select(m => (int?)m.MedicalRecordId)
            .FirstOrDefault()
    })
    .ToListAsync();

        PatientResults.AddRange(students);
        PatientResults.AddRange(communityPatients);
        var studentIds = students
            .Where(s => s.StudentId.HasValue)
            .Select(s => s.StudentId!.Value)
            .ToList();

        StudentClinicPatientIds = await _context.ClinicPatients
            .Where(cp => cp.StudentId.HasValue &&
                         studentIds.Contains(cp.StudentId.Value))
            .ToDictionaryAsync(
                cp => cp.StudentId!.Value,
                cp => cp.ClinicPatientId);

        var studentClinicPatientIds = StudentClinicPatientIds
            .Values
            .ToList();

        var studentDentalRecords = await _context.DentalRecords
            .Where(d => studentClinicPatientIds.Contains(d.ClinicPatientId))
            .OrderByDescending(d => d.VisitDate)
            .ToListAsync();

        foreach (var group in studentDentalRecords.GroupBy(d => d.ClinicPatientId))
        {
            DentalHistory[group.Key] = group.ToList();
        }
        var communityPatientIds = communityPatients
            .Where(p => p.ClinicPatientId.HasValue)
            .Select(p => p.ClinicPatientId!.Value)
            .ToList();


        var communityDentalHistory = await _context.DentalRecords
            .Include(d => d.ClinicPatient)
            .Where(d => communityPatientIds.Contains(d.ClinicPatientId))
            .OrderByDescending(d => d.VisitDate)
            .GroupBy(d => d.ClinicPatientId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.ToList());

        foreach (var group in communityDentalHistory)
        {
            DentalHistory[group.Key] = group.Value;
        }


        if (PatientResults.Count == 0)
        {
            StudentNotFound = true;
            return;
        }

        StudentNotFound = false;

        // Preserve the existing student workflow when
        // the search produces exactly one student result.
        var studentResult = PatientResults
            .FirstOrDefault(r =>
                r.PatientType == "Student" &&
                r.StudentId.HasValue);

        if (studentResult != null)
        {
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentResult.StudentId.Value);

            if (Student != null)
            {
                var medicalRecord = await _context.MedicalRecords
                    .FirstOrDefaultAsync(
                        m => m.StudentId == Student.StudentId);
                MedicalRequirements = await _context.MedicalRequirements
    .Where(r => r.StudentId == Student.StudentId)
    .OrderBy(r => r.RequirementName)
    .ToListAsync();

                if (MedicalRequirements.Count == 0)
                {
                    MedicalRequirements = new List<Models.MedicalRequirement>
    {
        new Models.MedicalRequirement
        {
            StudentId = Student.StudentId,
            RequirementName = "CBC (Complete Blood Count)",
            Status = "Not Submitted"
        },
        new Models.MedicalRequirement
        {
            StudentId = Student.StudentId,
            RequirementName = "Urinalysis",
            Status = "Not Submitted"
        },
        new Models.MedicalRequirement
        {
            StudentId = Student.StudentId,
            RequirementName = "Chest X-ray",
            Status = "Not Submitted"
        },
        new Models.MedicalRequirement
        {
            StudentId = Student.StudentId,
            RequirementName = "Drug Testing",
            Status = "Not Submitted"
        }
    };

                    _context.MedicalRequirements.AddRange(MedicalRequirements);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var oldRequirements = MedicalRequirements.ToList();

                    foreach (var requirement in oldRequirements)
                    {
                        switch (requirement.RequirementName)
                        {
                            case "Laboratory Result":
                                requirement.RequirementName = "CBC (Complete Blood Count)";
                                break;

                            case "X-Ray":
                                requirement.RequirementName = "Chest X-ray";
                                break;

                            case "Medical Certificate":
                            case "Physical Examination":
                                _context.MedicalRequirements.Remove(requirement);
                                break;
                        }
                    }

                    var existingNames = oldRequirements
                        .Where(r => _context.Entry(r).State != EntityState.Deleted)
                        .Select(r => r.RequirementName)
                        .ToHashSet();

                    var newRequirementNames = new[]
                    {
        "CBC (Complete Blood Count)",
        "Urinalysis",
        "Chest X-ray",
        "Drug Testing"
    };

                    foreach (var name in newRequirementNames)
                    {
                        if (!existingNames.Contains(name))
                        {
                            _context.MedicalRequirements.Add(
                                new Models.MedicalRequirement
                                {
                                    StudentId = Student.StudentId,
                                    RequirementName = name,
                                    Status = "Not Submitted"
                                });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                if (medicalRecord != null)
                {
                    HasMedicalRecord = true;
                    MedicalRecordId = medicalRecord.MedicalRecordId;
                }

                StudentAccount = await _context.StudentAccounts
                    .FirstOrDefaultAsync(
                        a => a.StudentId == Student.StudentId);
            }
        }
    }
}