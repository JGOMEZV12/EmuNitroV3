using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polar.Utilities;

namespace Polar.HabboHotel.Camera
{
    public class CameraHelper
    {
        // ✅ FIX #1: BASE_URL y BASE_URL2 eran idénticos — unificado en uno solo.
        //   Propiedad en lugar de campo para soportar recarga de config en caliente.
        public static string BASE_URL => PolarEnvironment.GetConfig().data["Url_camera"];

        // ✅ FIX #2: HttpClient estático compartido — crear uno por request agota sockets.
        //   Timeout de 10s para no bloquear el hilo indefinidamente si la cámara no responde.
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // ✅ FIX #3: Método async para no bloquear el hilo del handler de paquetes.
        //   Antes GetResponse() congelaba el hilo hasta que el servidor respondía.
        // ✅ FIX #4: REQUEST_URL era estático, el random se generaba una sola vez en startup
        //   y nunca cambiaba. Ahora el cache-buster se genera en cada llamada.
        // ✅ FIX #5: Nancy.Json.JavaScriptSerializer reemplazado por Newtonsoft.Json,
        //   que ya se usa en el resto del proyecto.
        public static async Task<string> RequestAsync(string type, int userId, int roomId, string base64)
        {
            string cacheBuster = RandomNumber.GenerateRandom(1, 99999).ToString();
            string url = BASE_URL + "camera.php?" + cacheBuster;

            var payload = new
            {
                type,
                user_id = userId,
                room_id = roomId,
                base_64 = base64,
                timestamp = PolarEnvironment.GetUnixTimestamp()
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 PlusEmulator/1.0");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}