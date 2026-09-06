namespace GestorPDV.App;

/// <summary>
/// Sem lógica de startup aqui de propósito — ver Program.cs (StartupObject
/// no .csproj). Base qualificada como global::System.Windows.Application
/// (não só "Application" com using System.Windows) porque este namespace
/// (GestorPDV.App) está aninhado dentro de GestorPDV, e GestorPDV.Application
/// (o projeto de camada de aplicação) é irmão dele — "Application" sem
/// qualificação resolve pro namespace GestorPDV.Application primeiro
/// (busca em namespace envolvente vence using directive), não pro tipo WPF.
/// </summary>
public partial class App : global::System.Windows.Application
{
}
