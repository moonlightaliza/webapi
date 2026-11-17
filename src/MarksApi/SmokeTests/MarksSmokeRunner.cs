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

        // 2. call every endpoint with a valid request
        await Check(client, "GET", "/health", 200, token: teacherToken);
        var createRes = await Check(client, "POST", "/api/students", 201,
            new{ registrationNo = "SMOKE-001", fullName = "Aliza Zia", email = "alizazia33@gmail.com" }, token: teacherToken);
        var studentId = await ReadIdAsync(createRes);
        await Check(client, "GET", $"/api/students/{studentId}", 200, token: teacherToken);

        // 3. call the failing cases
        // 4. delete everything whose name starts with "SMOKE-"
        await Check(client, "DELETE", $"/api/students/{studentId}", 204, token: teacherToken);

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
    //Helper function
    private static async Task<int> ReadIdAsync(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }
}