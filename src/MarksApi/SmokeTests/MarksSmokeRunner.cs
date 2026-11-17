using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace MarksApi.SmokeTests;

public static class MarksSmokeRunner
{
    private static int _failures;

    public static async Task<int> RunAsync(string baseUrl, IConfiguration config)
    {
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        // 1. log in using usernames and passwords read from config (secrets), never in code
        var teacherToken = await LoginAsync(client, config["Smoke:TeacherUsername"], config["Smoke:TeacherPassword"], "teacher");
        var studentToken = await LoginAsync(client, config["Smoke:StudentUsername"], config["Smoke:StudentPassword"], "student");
        if (teacherToken is null || studentToken is null)
        {
            Console.WriteLine("Login failed, stopping.");
            return 1;
        }

        int? studentId = null, courseId = null, enrollmentId = null;

        try
        {
            // 2. call every endpoint with a valid request
            await Check(client, "GET", "/health", 200, token: teacherToken);

            // students
            var studentBody = new { registrationNo = "SMOKE-001", fullName = "SMOKE-Student", email = "smoke-001@example.com" };
            var studentRes = await Check(client, "POST", "/api/students", 201, studentBody, teacherToken);
            studentId = await ReadIdAsync(studentRes);
            await Check(client, "GET", $"/api/students/{studentId}", 200, token: teacherToken);
            await Check(client, "PUT", $"/api/students/{studentId}", 200,
                new { registrationNo = "SMOKE-001", fullName = "SMOKE-Student Updated", email = "smoke-001@example.com" }, teacherToken);
            await Check(client, "GET", "/api/students", 200, token: teacherToken);

            // courses
            var courseBody = new { code = "SMOKE-C1", title = "SMOKE-Course", creditHours = 3 };
            var courseRes = await Check(client, "POST", "/api/courses", 201, courseBody, teacherToken);
            courseId = await ReadIdAsync(courseRes);
            await Check(client, "GET", $"/api/courses/{courseId}", 200, token: teacherToken);
            await Check(client, "PUT", $"/api/courses/{courseId}", 200,
                new { code = "SMOKE-C1", title = "SMOKE-Course Updated", creditHours = 3 }, teacherToken);
            await Check(client, "GET", "/api/courses", 200, token: teacherToken);

            // enrollments
            var enrollRes = await Check(client, "POST", $"/api/courses/{courseId}/enrollments", 201,
                new { studentId, courseId }, teacherToken);
            enrollmentId = await ReadIdAsync(enrollRes);
            await Check(client, "PUT", $"/api/enrollments/{enrollmentId}/mark", 200, new { mark = 80 }, teacherToken);
            await Check(client, "GET", $"/api/courses/{courseId}/enrollments", 200, token: teacherToken);

            // 3. call the failing cases
            // students
            await Check(client, "POST", "/api/students", 401, studentBody);                                   // no token
            await Check(client, "POST", "/api/students", 403, studentBody, studentToken);                     // student on Teacher endpoint
            await Check(client, "GET", "/api/students/999999", 404, token: teacherToken);                     // missing id
            await Check(client, "POST", "/api/students", 400,
                new { registrationNo = "SMOKE-002", fullName = "", email = "smoke-002@example.com" }, teacherToken); // empty name
            await Check(client, "POST", "/api/students", 409, studentBody, teacherToken);                     // duplicate

            // courses
            await Check(client, "POST", "/api/courses", 400,
                new { code = "SMOKE-C2", title = "SMOKE-Bad", creditHours = 5 }, teacherToken);               // invalid creditHours
            await Check(client, "POST", "/api/courses", 409, courseBody, teacherToken);                       // duplicate code

            // enrollments
            await Check(client, "POST", $"/api/courses/{courseId}/enrollments", 409, new { studentId, courseId }, teacherToken);  // duplicate enroll
            await Check(client, "PUT", $"/api/enrollments/{enrollmentId}/mark", 400, new { mark = 101 }, teacherToken); // mark out of range
        }
        catch (Exception ex)
        {
            _failures++;
            Console.WriteLine($"FAIL unexpected error: {ex.Message}");
        }
        finally
        {
            // 4. delete everything whose name starts with "SMOKE-"
            // order matters: enrollment first, then student, then course (otherwise 409)
            if (enrollmentId is not null)
                await Check(client, "DELETE", $"/api/enrollments/{enrollmentId}", 204, token: teacherToken);
            if (studentId is not null)
                await Check(client, "DELETE", $"/api/students/{studentId}", 204, token: teacherToken);
            if (courseId is not null)
                await Check(client, "DELETE", $"/api/courses/{courseId}", 204, token: teacherToken);
        }

        Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECK(S) FAILED");
        return _failures == 0 ? 0 : 1;
    }

    private static async Task<string?> LoginAsync(HttpClient client, string? username, string? password, string role)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine($"Missing {role} credentials in config.");
            return null;
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Login as {role} failed with {(int)response.StatusCode}.");
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString();
    }

    private static async Task<HttpResponseMessage> Check(HttpClient client, string method, string path, int expected, object? body = null, string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        var actual = (int)response.StatusCode;
        var ok = actual == expected;
        if (!ok) _failures++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {method} {path} expected {expected} got {actual}");
        return response;
    }

    // Returns null when the create failed, so cleanup only deletes what exists
    private static async Task<int?> ReadIdAsync(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }
}