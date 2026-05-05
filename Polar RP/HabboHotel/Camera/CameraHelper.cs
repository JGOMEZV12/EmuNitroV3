using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polar.Utilities;

namespace Polar.HabboHotel.Camera
{
    public class CameraHelper
    {
        public static string BASE_URL => PolarEnvironment.GetConfig().data["Url_camera"];
        private const string PHP_SCRIPT = "/var/www/html/wwwswfs/newfoto/camera.php";
        private const string PHP_BIN = "/usr/bin/php";

        public static async Task<string> RequestAsync(string type, int userId, int roomId, string base64)
        {
            var payload = new
            {
                type,
                user_id = userId,
                room_id = roomId,
                base_64 = base64,
                timestamp = PolarEnvironment.GetUnixTimestamp()
            };

            string json = JsonConvert.SerializeObject(payload);

            var psi = new ProcessStartInfo
            {
                FileName = PHP_BIN,
                Arguments = PHP_SCRIPT,
                RedirectStandardInput = true,   // ✅ enviamos JSON por stdin
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // ✅ Escribir JSON por stdin y cerrar para que PHP lo lea
            await process.StandardInput.WriteAsync(json);
            process.StandardInput.Close();

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"[Camera] camera.php error (exit {process.ExitCode}): {stderrTask.Result}");

            return stdoutTask.Result;
        }
    }
}