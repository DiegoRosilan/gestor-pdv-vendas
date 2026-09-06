using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorPDV.Vendas;

public sealed class AppConfig
{
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Modo de emissão fiscal configurado na tela Menu Fiscal (F8):
    /// "NENHUM" (padrão — nenhuma emissão real, só CFOP fixo em 5102),
    /// "HOMOLOGACAO" ou "PRODUCAO". Este app não fala com a SEFAZ/SAT de
    /// verdade em nenhum dos três modos — ver LEIA-ME.
    /// </summary>
    public string ModoFiscal { get; set; } = "NENHUM";

    [JsonIgnore]
    public string? CaminhoArquivo { get; private set; }

    public static AppConfig Load(string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"Arquivo de configuração não encontrado: {fullPath}", fullPath);

        var config = JsonSerializer.Deserialize<AppConfig>(
            File.ReadAllText(fullPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Configuração inválida.");

        config.CaminhoArquivo = fullPath;
        return config;
    }

    /// <summary>Regrava appsettings.json com os valores atuais (usado ao salvar a tela Menu Fiscal).</summary>
    public void Save()
    {
        if (CaminhoArquivo is null)
            throw new InvalidOperationException("Configuração não foi carregada de um arquivo — não há onde salvar.");

        File.WriteAllText(CaminhoArquivo, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
