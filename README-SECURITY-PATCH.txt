CAPSTONE CLINIC — SECURITY PATCH (APPLY AFTER THE RBAC PATCH)

This patch builds on the existing RBAC patch. It does not change the IoT
networking approach or add the deferred logout UI.

Included:
- Secure cookie defaults: HttpOnly, SameSite=Lax, 30-minute sliding expiration,
  and Secure cookies outside Development.
- Startup validation that the JWT signing key exists and is at least 32 bytes.
- Reduced JWT clock skew.
- Requires authentication for GET /api/iot/vital-signs/pending, which returns
  stored vital-sign readings. The current CMS sensor-reading page calls the
  ESP32 directly, so this does not change that direct-read flow.
- Constant-time comparison for IoT and integration API keys.
- Removes console output of raw sensor JSON, reading values, device URL, and
  JWT-key length.

IMPORTANT REMAINING SECURITY ACTION BEFORE HOSTING:
The source repository's appsettings.json contains JWT/API key values. Treat them
as exposed, especially if the repository was pushed to a remote. Rotate the JWT
key and integration key before staging/production. Rotate the IoT key only in
coordination with the firmware/config that sends it, so the device workflow is
not unexpectedly broken. Move all keys into .NET User Secrets locally and into
host environment variables/a secret store when hosted. Do not commit the new
values or paste them into public chats/issues.

The existing appsettings.json values are intentionally not deleted by this
patch so that the working local configuration is not silently broken. This is
not a production-ready secrets configuration until the rotation/migration above
is completed.

VALIDATION AFTER APPLYING:
1. Run: dotnet build .\Capstone-Clinic\Capstone-Clinic.csproj
2. Test login and role restrictions from the RBAC patch.
3. Verify an unauthenticated request to /api/iot/vital-signs/pending is rejected.
4. Verify valid/invalid IoT POST and integration API-key requests behave as expected.
5. Verify the existing CMS direct ESP32 reading workflow still works.
6. Before any hosted test, rotate/migrate secrets and test against synthetic data.

The build was not run in this packaging environment because the .NET SDK is not
installed here; run the build on the Windows development machine.
