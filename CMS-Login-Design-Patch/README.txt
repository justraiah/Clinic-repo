CMS LOGIN PAGE + FAVICON UI PATCH

Patch files are relative to the Capstone-Clinic ASP.NET Core project root:

1. Pages/Login.cshtml
   Replaces the existing login page markup/styles only. The Razor Page model,
   form method, Username/Password tag helpers, and ErrorMessage display remain.

2. Pages/Shared/_Layout.cshtml
   Adds favicon references inside the existing HTML head. Other layout content
   is preserved from the supplied project snapshot.

3. wwwroot/favicon.svg
   New green medical-heart/ECG favicon.

APPLY
- Extract this patch ZIP.
- Copy the contents of the Capstone-Clinic folder into the actual project folder
  (the folder containing the Capstone-Clinic.csproj), allowing overwrite of
  Pages/Login.cshtml and Pages/Shared/_Layout.cshtml.
- Keep wwwroot/favicon.svg alongside the existing wwwroot/favicon.ico.
- Run/build the project and verify the login page at desktop and mobile sizes.
- Hard-refresh the browser or clear cached favicon if the old tab icon remains.

Login.cshtml.cs and authentication logic are intentionally not included or changed.
Build validation was not run in this environment (dotnet CLI unavailable).
