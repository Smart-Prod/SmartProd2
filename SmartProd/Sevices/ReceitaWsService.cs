using System.Text.RegularExpressions;
using SmartProd.Models;
using Newtonsoft.Json;

namespace SmartProd.Sevices
{
    public class ReceitaWsService
    {
        private readonly HttpClient _httpClient;

        public ReceitaWsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DadosReceitaWs> ConsultarCnpjAsync(string cnpj)
        {
            try
            {
                cnpj = Regex.Replace(cnpj, "[^0-9]", ""); // remove pontos e traços
                string url = $"https://www.receitaws.com.br/v1/cnpj/{cnpj}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Erro ao consultar CNPJ.");

                var json = await response.Content.ReadAsStringAsync();

                var dados = JsonConvert.DeserializeObject<DadosReceitaWs>(json);

                if (dados.Status?.ToLower() != "ok")
                    throw new Exception($"Erro ao buscar CNPJ: {dados.Message}");

                return dados;
            }
            catch (Exception ex)
            {
                // logar se necessário
                throw new Exception($"Erro ao consultar CNPJ: {ex.Message}");
            }
        }
    }
}
