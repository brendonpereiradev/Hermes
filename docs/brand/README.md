# Marca do Hermes

Hermes é o nome de produto do PrinterInstall desde a versão 1.6.0, e o repositório no GitHub também se chama Hermes (o endereço antigo, `.../PrinterInstall`, redireciona). Os projetos `PrinterInstall.*`, a pasta `%LocalAppData%\PrinterInstall` e os scripts continuam com o nome antigo; o resto da marca visível mudou (janelas, ícone, executável `Hermes.exe`, página de teste e documentação).

| Arquivo | Uso |
| --- | --- |
| `hermes-mark.svg` | Símbolo quadrado (ícone, avatar). |
| `hermes-logo-dark.svg` | Símbolo + nome sobre fundo escuro `#101318`. |
| `hermes-logo-light.svg` | Símbolo + nome com fundo transparente, para superfícies claras. |
| `hermes-logo-animated.svg` | Versão escura animada (~2,5 s, toca uma vez): a folha voa, as linhas CMY se imprimem e o nome entra. Com `prefers-reduced-motion` mostra a logo estática. |

No aplicativo, o emblema vive em `src/PrinterInstall.App/Themes/Brand.xaml` em três desenhos (completo, 24–32 px e 16 px). `scripts/New-HermesIcons.ps1` gera `Assets/app.ico` e `Assets/app.png` a partir desse arquivo; rode-o depois de mudar o emblema. Enquanto há trabalho em andamento (implantação, controle de impressoras, teste de impressora), o emblema da barra de título voa (`Controls/HermesEmblem.xaml`), e para quando o Windows está com as animações desligadas.

## Conceito

Hermes é o terceiro da família, ao lado do Hefesto (a forja que monitora as impressoras) e do Talos (o guardião que vigia o servidor).

- **Asa dourada**: Hermes, o mensageiro dos deuses, que leva a impressora até cada computador do domínio.
- **Folha com linhas ciano, magenta e amarelo**: a impressão, herdada das faíscas CMY do Hefesto.
- **Ladrilho grafite com borda azul**: a moldura comum ao Talos e ao Hefesto.

## Cores e tipografia

| Papel | Escuro (base Talos) | Claro (base Hefesto) |
| --- | --- | --- |
| Fundo da janela | `#101318` | `#F1F4F8` |
| Painel | `#181C23` | `#FFFFFF` |
| Campo | `#0E1116` | `#F7F9FC` |
| Linha | `#262C36` | `#DDE3EB` |
| Ação | `#4C8DF6` | `#2F6FE0` |
| Texto | `#E6E9EE` | `#111A2C` |

Todos os neutros ficam no mesmo matiz azul-ardósia, em três profundidades: no escuro o campo é mais fundo que a janela, que é mais funda que o painel; no claro, a janela é a mais escura e o painel, branco. Os controles do WPF UI (campos, listas, caixas de seleção, cabeçalho do log) usam essas mesmas cores em `Themes/ThemeTokens.*.xaml`, e `Controls/HermesWindow.cs` impede que o WPF UI pinte a janela com o cinza neutro dele.

A asa usa `#E8A05C` só na marca, para não ser confundida com os avisos amarelos. Os estados usam ponto e texto coloridos: concluído `#3DD68C`, em andamento `#4C8DF6`, atenção `#F2C14E`, erro `#F0605D`, revertido `#2DD4BF` (tons equivalentes no tema claro).

A tipografia tem três papéis fixos, todos com fontes que já vêm no Windows:

| Fonte | Onde |
| --- | --- |
| Segoe UI | Toda a interface: títulos, rótulos, botões, mensagens, textos de exemplo e contadores (em SemiBold). |
| Bahnschrift | Só o nome HERMES, em caixa alta espaçada (o par do Barlow Condensed do Talos). |
| Consolas | Dados que se digitam ou leem como valor (nome de computador, IP, log) e o slogan "IMPLANTAÇÃO DE IMPRESSORAS". |

Textos de instrução nunca usam a Consolas, mesmo dentro de um campo de dados: na caixa de computadores, o exemplo é um texto sobreposto em Segoe UI, porque o placeholder do WPF UI herdaria a fonte do campo. Nos SVGs os textos estão como `<text>` com fontes de reserva; para impressão, converta-os em contornos.

## Barra de título

A barra de título do WPF UI tem 48 px, mas os botões de minimizar, maximizar e fechar vêm com 30 px, presos no topo. `Controls/HermesWindow.cs` estica esses botões para a altura da barra, e as ferramentas (tema, configurações) usam `TitleBarToolButtonStyle`, com 44 × 48 px. Assim o emblema, o nome, as ferramentas e os botões da janela ficam na mesma linha.
