<h1 align="center">PrinterInstall</h1>

<p align="center">
  Aplicativo desktop para Windows que instala e padroniza impressoras de rede e etiquetadoras térmicas em estações de trabalho.
</p>

<p align="center">
  <strong>Português</strong> · <a href="README.en.md">English</a>
</p>

<p align="center">
  <img src="docs/screenshots/login.png" alt="Tela de autenticação" width="34%" />
  &nbsp;
  <img src="docs/screenshots/main.png" alt="Tela principal de implantação" width="60%" />
</p>

---

## Sobre o projeto

PrinterInstall é uma aplicação em .NET 8 e WPF que automatiza a configuração de impressoras de rede em ambientes corporativos e hospitalares. Ela cria portas TCP/IP, instala drivers e configura filas em vários computadores remotamente, sem configurar cada máquina à mão.

A solução tem duas camadas: `PrinterInstall.Core` (catálogo de drivers, autenticação LDAP, orquestração e operações remotas via WMI/CIM) e `PrinterInstall.App` (interface MVVM com WPF-UI). Cada recurso criado é registrado em um diário de reversão, e o orquestrador desfaz as etapas concluídas se a instalação falhar ou for cancelada.

---

## Funcionalidades

| Funcionalidade | Descrição |
|---|---|
| Instalação em lote | Instala filas em vários computadores ao mesmo tempo, com status em tempo real. |
| Reversão automática | Remove portas e filas criadas quando a instalação falha em uma estação. |
| Assistente de controle | Lista, renomeia e remove filas em computadores remotos ou na máquina local. |
| Teste de comunicação | Imprime páginas ou etiquetas de teste direto pela porta raw 9100 antes de instalar. |
| Etiquetas Gainscha | Configura os tamanhos Paciente (89 × 36 mm), Matrix (50 × 30 mm), Pulseira (25 × 270 mm) e Lote (45 × 13 mm). |
| Autenticação por domínio | Valida credenciais no Active Directory via LDAP, nos formatos UPN ou NetBIOS. |
| Relatórios | Exporta um log com o resultado de cada operação. |

---

## Stack

- **.NET 8 e WPF com WPF-UI**: aplicativo Windows com interface Fluent.
- **CommunityToolkit.Mvvm e Microsoft.Extensions.Hosting**: MVVM e injeção de dependência.
- **System.Management**: chamadas WMI/CIM para portas, drivers e filas do spooler.
- **System.DirectoryServices.Protocols**: autenticação LDAP no Active Directory.
- **Seagull SSDAL e SDS**: calibração de mídia nas impressoras térmicas Gainscha.
- **xUnit e Moq**: testes automatizados.

---

## Como executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Windows 10, Windows 11 ou Windows Server.
- Credenciais com privilégio administrativo nos computadores de destino.

### Passo a passo

1. Clone o repositório e compile a solução:

   ```powershell
   git clone https://github.com/brendonpereiradev/PrinterInstall.git
   cd PrinterInstall
   dotnet build PrinterInstall.sln
   ```

2. Inicie o aplicativo:

   ```powershell
   dotnet run --project src/PrinterInstall.App
   ```

3. Na primeira execução, configure o domínio e o servidor LDAP no ícone de configurações e entre com `usuario@dominio` ou `DOMINIO\usuario`.

### Comandos disponíveis

| Comando | Descrição |
|---|---|
| `dotnet build PrinterInstall.sln` | Compila a solução. |
| `dotnet run --project src/PrinterInstall.App` | Inicia o aplicativo. |
| `dotnet test PrinterInstall.sln` | Executa os testes. |
| `pwsh scripts/Publish-PrinterInstall.ps1` | Gera o executável único em `publish/PrinterInstall`. |
| `python scripts/Test-PublicData.py` | Confere que o repositório não contém dados reais de ambiente. |
| `pwsh scripts/Test-Secrets.ps1` | Procura segredos antes de enviar alterações. |

---

## Documentação

- [Documentação (PDF)](docs/Documenta%C3%A7%C3%A3o.pdf)
- [Documentação (MD)](docs/Documenta%C3%A7%C3%A3o.md)

---

## Licença

Distribuído sob a licença MIT. Consulte o arquivo [LICENSE](LICENSE).

Os drivers e utilitários de terceiros incluídos pertencem aos seus fabricantes e seguem os próprios termos de licença.
