# Hermes: documentação

Guia de uso do Hermes (antigo PrinterInstall) 1.9.1 para técnicos de suporte, helpdesk e analistas de campo. Os nomes, domínios e endereços usados como exemplo são fictícios.

Na versão 1.6.0 o aplicativo passou a se chamar Hermes e ganhou um novo visual, alinhado aos projetos Talos e Hefesto. O executável agora é `Hermes.exe`; configurações e logs continuam na pasta `%LocalAppData%\PrinterInstall`, então quem já usava o aplicativo não perde nada.

Também disponível em [PDF](Documenta%C3%A7%C3%A3o.pdf).

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Instalação e primeira execução](#2-instalação-e-primeira-execução)
3. [Login e configuração de domínio](#3-login-e-configuração-de-domínio)
4. [Implantação de impressoras em lote](#4-implantação-de-impressoras-em-lote)
5. [Controle de impressoras](#5-controle-de-impressoras)
6. [Testar impressora](#6-testar-impressora)
7. [Tamanhos de etiqueta Gainscha](#7-tamanhos-de-etiqueta-gainscha)
8. [Logs e diagnóstico](#8-logs-e-diagnóstico)
9. [Solução de problemas](#9-solução-de-problemas)

---

## 1. Visão geral

O Hermes instala e padroniza impressoras de rede e etiquetadoras térmicas em estações Windows, na própria máquina ou em computadores remotos. Em uma única execução ele:

- identifica a impressora no IP informado e bloqueia a instalação se o modelo real não corresponder ao selecionado;
- instala o driver homologado para a marca escolhida;
- cria a porta TCP/IP (raw 9100) e a fila de impressão com o nome definido por você;
- aplica o tamanho de etiqueta correto nas impressoras térmicas Gainscha;
- desfaz automaticamente o que criou se a instalação falhar ou for cancelada (reversão).

Além da implantação, o aplicativo oferece o **Controle de impressoras** (consultar, remover e renomear filas e reiniciar o spooler) e o **Testar impressora** (envio de página de teste direto para o equipamento).

### Aparência

- O aplicativo tem tema escuro e tema claro. O ícone de sol/lua, no canto superior direito, alterna entre eles e a escolha fica salva.
- Enquanto uma implantação, o Controle de impressoras ou o Testar impressora estão trabalhando, o emblema do Hermes "voa" na barra de título: a asa bate e as linhas coloridas da folha se imprimem. Quando o trabalho termina, ele para.
- Se a opção **Mostrar animações no Windows** estiver desligada, o emblema fica sempre parado.

### Pré-requisitos

- Windows 10, Windows 11 ou Windows Server.
- Conta de domínio com privilégio administrativo nos computadores de destino. Para usar em casa, sem domínio, basta uma conta do Windows que possa confirmar a elevação (UAC) na própria máquina; veja [Uso local, sem domínio](#uso-local-sem-domínio).
- Nome de rede ou IP de cada computador de destino (ex.: `PC-RECEPCAO-01` ou `192.0.2.120`).
- IP fixo ou reservado da impressora (ex.: `192.0.2.50`), acessível na porta 9100.
- Para computadores remotos: acesso de gerenciamento (RPC/WMI e SMB) liberado no firewall.

### Marcas suportadas

| Opção no aplicativo | Uso típico | Driver utilizado | Modelos testados |
|---|---|---|---|
| Epson | Jato de tinta e multifuncionais | EPSON Universal Print Driver | EcoTank M1180, WorkForce Pro WF-M5899, WF-M5799, WF-C5890, WF-C5790 |
| Lexmark | Laser para receitas e prontuários | Lexmark Universal v4 XL / v2 XL | CX532adwe, MX622adhe |
| Brother | Laser de posto e administrativo | Brother HL-L5212DW Printer | HL-L5212DW |
| Gainscha | Etiquetas térmicas | Gainscha GA-2408T (Seagull) | GA-2408T |

Outros modelos da mesma fabricante podem funcionar, mas não foram homologados.

Para a Lexmark, o aplicativo aceita também as famílias atuais que o *Lexmark UPD Technical White Paper Rev. 3a* (UPD 3.1.0.0) lista sem restrições, comparando a família do modelo sem o sufixo de configuração (ex.: MX622ade, MX622adhe e MX622adthe contam todos como MX622):

- **Laser mono:** MS321, MS331, MS421, MS431, MS521, MS531, MS621, MS622, MS631, MS632, MS725, MS821–MS826, B2338, B2442, B2546, B2650, B2865, B3340, B3442.
- **Multifuncional mono:** MX321, MX331, MX421, MX431, MX432, MX521, MX522, MX532, MX622, MX632, MX721, MX722, MX725, MX822, MX824, MX826, MX931, MB2338, MB2442, MB2546, MB2650, MB2770, MB3442.
- **Laser colorida:** CS331, CS421, CS431, CS521, CS531, CS622, CS632, CS720, CS725, CS727, CS728, CS730, CS735, CS737, CS820, CS827, CS921, CS923, CS927, CS943, CS963, C2240, C2326, C2335, C2425, C2535, C3224, C3326, C3426, C4342, C4352.
- **Multifuncional colorida:** CX331, CX421, CX431, CX522, CX532, CX622, CX625, CX635, CX725, CX727, CX730, CX735, CX737, CX820, CX825, CX827, CX833, CX860, CX920–CX924, CX927, CX961–CX963, MC2325, MC2425, MC2535, MC2640, MC3224, MC3326, MC3426.

Séries antigas com recursos limitados no driver (MS/MX/CS/CX 310–910, E, T, X) e a linha B2236/MB2236 continuam bloqueadas até serem testadas.

---

## 2. Instalação e primeira execução

### Usando o executável

Baixe `Hermes.exe` na página de releases do repositório e copie-o para uma pasta local. O arquivo é único e não exige instalação. Para operar na própria máquina, abra com o botão direito e escolha **Executar como administrador**.

### Compilando a partir do código-fonte

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/brendonpereiradev/Hermes.git
cd Hermes
dotnet build PrinterInstall.sln
dotnet run --project src/PrinterInstall.App
```

Para gerar o executável único em `publish\PrinterInstall`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-PrinterInstall.ps1 -Configuration Release
```

---

## 3. Login e configuração de domínio

<p align="center"><img src="screenshots/login.png" alt="Tela de login" width="38%" /></p>

Informe seu usuário e senha de rede e clique em **Entrar**. O campo Usuário aceita:

| Formato | Exemplo | Observação |
|---|---|---|
| UPN | `suporte.ti@empresa.test` | Domínio informado no próprio usuário. |
| NetBIOS | `EMPRESA\suporte.ti` | Domínio informado no próprio usuário. |
| Só o usuário | `suporte.ti` | Usa o domínio padrão salvo nas Configurações. |

- **Lembrar-me** guarda apenas o nome de usuário. A senha fica somente na memória durante a execução e nunca é gravada em disco.
- O ícone de olho mostra ou oculta a senha; o ícone de sol/lua alterna entre tema claro e escuro.
- O ícone de computador, na barra de título, entra sem domínio para uso local (veja abaixo).
- **Lembrar-me** e o tema escolhido valem para as próximas aberturas do aplicativo.
- Enquanto o domínio valida a senha, o botão **Entrar** vira **Cancelar**. Clique nele ou pressione **Esc** para voltar ao formulário na hora, por exemplo quando perceber que digitou a senha errada. A tentativa já enviada ao domínio ainda conta como uma tentativa de senha.
- Em caso de erro, a tela mostra a causa (senha incorreta, conta bloqueada, domínio não encontrado, sem rede/VPN) e o link **Ver detalhes técnicos**.

### Uso local, sem domínio

Para instalar uma impressora em casa ou em um computador fora do domínio, clique no ícone de computador na barra de título da tela de login, à esquerda do sol/lua e da engrenagem. Passando o mouse, aparece a dica **Usar localmente**. Não é preciso usuário, senha nem domínio configurado.

- A sessão local só trabalha nesta máquina. Na implantação e no Controle de impressoras, a lista de computadores já vem com o nome deste PC e não pode ser editada.
- As operações rodam com a conta do Windows em uso. Quando uma etapa precisa de privilégio administrativo, o Windows pede confirmação (UAC); a conta precisa ser administradora ou saber a senha de um administrador.
- O relatório de log identifica o operador como `COMPUTADOR\usuário` do Windows.
- Para voltar a trabalhar em computadores remotos, feche o aplicativo e entre com uma conta de domínio.

### Configurações de domínio e rede

O executável não vem com domínio de nenhuma empresa. Na primeira execução, clique no ícone de engrenagem da tela de login:

1. **Domínio padrão**: nome do domínio Active Directory (ex.: `empresa.test` ou `EMPRESA`). O botão **Detectar domínio** preenche com o domínio da máquina atual.
2. **Servidor / host LDAP alternativo** (opcional): IP ou FQDN de um controlador de domínio, para quando o DNS local não resolve o domínio. Deixe em branco para usar o DNS.
3. Clique em **Salvar**. **Restaurar padrão** limpa os valores.

As configurações valem de imediato e ficam em `%LocalAppData%\PrinterInstall\settings.json`, apenas neste computador.

---

## 4. Implantação de impressoras em lote

<p align="center"><img src="screenshots/main.png" alt="Janela principal" width="90%" /></p>

### 4.1 Computadores alvo

- Por padrão, o seletor fica em **Um computador**: digite o IP ou nome de rede de uma única máquina, como no campo IP da impressora.
- Para vários computadores, escolha **Lista** e informe um nome ou IP por linha. Colar uma lista com mais de um nome no campo único passa para **Lista** automaticamente.
- Cada modo guarda o próprio texto; a implantação usa o modo selecionado. Ao trocar de modo com o outro campo vazio, o computador já digitado é levado junto.
- **Adicionar este PC** inclui a máquina local (marcada como "(este PC)"). Se o campo único já tiver outro computador, os dois vão para **Lista**.
- Na sessão local (sem domínio), o campo já vem com este PC e fica travado; um nome remoto termina em erro sem ser contatado.
- O contador ao lado do título mostra quantos computadores foram reconhecidos e quantos estão inválidos.

### 4.2 Configuração de impressora

Cada bloco do painel direito define uma fila que será criada em todos os computadores alvo:

1. **Modelo**: Epson, Lexmark, Brother ou Gainscha.
2. **IP**: endereço IP ou hostname da impressora (ex.: `192.0.2.50`).
3. **Nome da impressora**: nome da fila no Windows, de preferência padronizado (ex.: `POSTO-ENF-LASER-01`, `RECEPCAO-EPSON-01`, `TRIAGEM-ETIQ-PULSEIRA`).
4. **Tamanho de etiqueta** (apenas Gainscha, obrigatório): Paciente, Matrix, Pulseira ou Lote. Veja a [seção 7](#7-tamanhos-de-etiqueta-gainscha).

Use **Adicionar impressora** para criar mais filas na mesma execução e **Remover** para excluir um bloco. Marque **Imprimir teste** para que cada fila criada imprima uma página de teste ao final.

### 4.3 Verificações antes de implantar

Ao clicar em **Instalar**, o aplicativo valida os dados e pode exibir avisos:

| Aviso | Quando ocorre | O que fazer |
|---|---|---|
| Inversão de campos detectada | O nome parece um IP e o IP parece um nome. | **Inverter e instalar** corrige automaticamente; **Cancelar e corrigir** volta ao formulário. |
| Possível divergência de modelo | O nome da fila sugere outro tipo (ex.: ETIQ ou PULSEIRA com marca de folha comum). | Confira a marca. **Prosseguir mesmo assim** ou **Cancelar e corrigir**. |
| Validação da impressora bloqueou a instalação | A impressora no IP informado é de outra marca/modelo, ou não pôde ser identificada com segurança. | Confira o IP e o modelo selecionado. Nenhuma alteração é feita nas estações. |

As caixas de aviso e de resumo não têm barra de título: feche pelos botões da própria caixa ou com **Esc**, e arraste pelo corpo da caixa para movê-la.

### 4.4 Acompanhamento

Os computadores são processados em paralelo. A tabela **Status** mostra, para cada computador e impressora, o estado atual. Embaixo do estado aparece um detalhe em cinza quando ele acrescenta algo: a etapa em andamento ("Criando porta…") ou o motivo de uma falha ("Impressora offline: não respondeu ao ping"). Uma falha em uma máquina não interrompe as demais.

Ao lado do título **Status**, o resumo mostra quantas filas foram concluídas sobre o total (ex.: "1 de 3 concluídos"). A barra à direita divide o lote em concluídos (verde), com falha (vermelho), terminados sem alteração (cinza: já existentes, cancelados ou revertidos) e o que ainda está em andamento, que brilha enquanto a implantação roda. Ao fim do lote, a barra fica cheia.

Cada estado aparece com um ponto colorido: cinza para pendente, azul para em andamento, verde para concluído, amarelo para concluído com aviso, já existe ou cancelado, vermelho para erro e verde-água para revertido.

| Estado | Significado |
|---|---|
| Pendente | Aguardando início. |
| Identificando / Validando impressora | Consulta ao equipamento no IP informado e comparação com o modelo selecionado. |
| Conectando | Conexão com o computador alvo. |
| Validando / Instalando / Confirmando driver | Verificação e instalação do pacote de driver no Windows. |
| Configurando | Criação da porta TCP/IP e da fila de impressão. |
| Concluído | Fila instalada e pronta para uso. |
| Concluído com aviso | Fila instalada, mas a página de teste falhou; o motivo aparece embaixo do estado. |
| Já existe | A fila já existia; nada foi alterado. |
| Driver ausente / Erro | Falha; o motivo aparece embaixo do estado e no log. |
| Revertendo: fila / Revertendo: porta / Revertido | Reversão em andamento ou concluída. Se a porta não pôde ser verificada ou removida, a linha fica **Revertido** e o aviso aparece embaixo do estado. |
| Cancelado | Interrompido pelo operador. |

Ao final, uma caixa de resumo responde logo no título se deu certo: **Instalação concluída** (ícone verde), **Instalação concluída com problemas** (âmbar), **A instalação falhou** (vermelho) ou **Instalação interrompida** (âmbar, após cancelamento). Abaixo, uma frase com as contagens que não são zero (ex.: "3 concluídos, 1 erro.") e uma linha por falha ou aviso, com computador, impressora e motivo.

### 4.5 Cancelamento e reversão automática

Clique uma vez em **Cancelar** durante a implantação. O aplicativo para as próximas etapas e remove as portas e filas criadas nesta execução. Uma chamada do Windows já em andamento pode precisar terminar antes da reversão: aguarde o resumo final.

> **Atenção:** se uma fila tiver sido criada no alvo depois do pedido de cancelamento, use o **Controle de impressoras** para concluir a limpeza.

---

## 5. Controle de impressoras

Abra pelo botão **Controle de impressoras** no topo da janela principal. O assistente serve para auditar, remover e renomear filas e para destravar a fila de impressão de uma estação.

### 5.1 Selecionar computadores

1. Informe um IP ou nome de computador por linha (Enter adiciona outra linha) ou use **Adicionar este PC**. Na sessão local, o campo já vem com este PC e fica travado.
2. Clique em **Avançar**.

### 5.2 Revisar cada computador

O assistente percorre os computadores um a um ("Computador 1 de N"). Para cada um ele testa ping e as portas de gerenciamento e mostra **Online** ou **Offline**:

- **Online**: a lista de impressoras é carregada com as colunas Remover, Impressora, Porta e Renomear para. Se a consulta falhar (por exemplo, senha recusada ou conta bloqueada), o motivo aparece abaixo do nome do computador; se o computador não tiver filas, o assistente avisa que nenhuma impressora foi encontrada.
- **Offline**: a resposta chega em cerca de 2 segundos. Ligue o computador e clique em **Testar novamente**, ou avance sem alterá-lo.

Em cada impressora, marque **Remover** para excluí-la ou digite um nome em **Renomear para**. Também é possível apenas consultar e avançar sem alterar nada.

### 5.3 Revisar o plano e executar

A última etapa lista as remoções e renomeações por computador. Clique em **Executar alterações**. Filas removidas têm suas portas TCP/IP apagadas quando ficam sem uso; renomeações são aplicadas sem reinstalar o driver. O **Log de execução** registra cada operação e pode ser exportado. Ao terminar, uma caixa de resumo mostra o resultado: **Alterações concluídas** (ícone verde) quando tudo deu certo, **Alterações concluídas com problemas** (âmbar) quando houve avisos ou falhas parciais e **As alterações falharam** (vermelho) quando nenhuma alteração deu certo ou ocorreu um erro inesperado.

### 5.4 Reiniciar spooler

Use quando documentos ficam presos com status "Excluindo..." ou bloqueiam novos envios. Com o computador online e sem operação em andamento, clique em **Reiniciar spooler** e confirme. O aplicativo para o serviço Spooler, apaga os trabalhos em `C:\Windows\System32\spool\PRINTERS`, inicia o serviço novamente e recarrega a lista de impressoras.

---

## 6. Testar impressora

Verifica a comunicação com a impressora antes de implantar, enviando um teste direto pela porta raw 9100, sem criar fila no Windows.

1. Clique em **Testar impressora…** no topo da janela principal.
2. Escolha o **Modelo** e informe o **IP**. Para Gainscha, escolha também o tamanho de etiqueta.
3. Clique em **Testar** e confirme em **Enviar teste**.
4. O aplicativo testa a conectividade, identifica o equipamento e envia o teste. Confira na impressora se ele saiu.

Epson, Lexmark e Brother imprimem uma folha (PCL) com data, hora e IP. A Gainscha imprime uma etiqueta (TSPL) nas medidas do tamanho escolhido.

---

## 7. Tamanhos de etiqueta Gainscha

| Tamanho | Largura × altura | Setor | Aplicação |
|---|---|---|---|
| Paciente | 89 × 36 mm | Recepção, triagem, postos | Identificação de leitos e prontuários |
| Matrix | 50 × 30 mm | Laboratório | Tubos de coleta e frascos |
| Pulseira | 25 × 270 mm | Triagem e internação | Pulseira de paciente com código de barras |
| Lote | 45 × 13 mm | Farmácia e almoxarifado | Unitização de doses e validade |

> Confirme sempre o rolo físico instalado na impressora antes de escolher o tamanho. Um tamanho maior que a etiqueta real faz a impressora pular etiquetas em branco.

---

## 8. Logs e diagnóstico

- **Abrir pasta de logs** abre `%LocalAppData%\PrinterInstall\Logs`.
- **Exportar log** salva um único arquivo .txt com resumo, histórico, contexto do Windows (usuário, elevação, domínio, rede, spooler, administradores locais, drivers e filas) e o log técnico da sessão. A senha é ocultada.
- A exportação fica disponível também durante a execução ou o cancelamento.

### Diagnóstico direto no computador com problema

1. Copie o executável para uma pasta local do computador e abra como administrador.
2. Faça login, clique em **Adicionar este PC** e deixe apenas essa máquina na lista.
3. Configure as impressoras, clique em **Instalar** e aguarde o resumo final, mesmo em caso de erro.
4. Clique em **Exportar log** antes de fechar e envie o .txt ao suporte.

---

## 9. Solução de problemas

**Não foi possível conectar ao computador alvo (WMI / CIM / RPC)**  
Estação desligada, sem rede, firewall bloqueando ou conta sem privilégio administrativo. Teste `ping PC-RECEPCAO-01` e confirme que a conta é administradora na máquina.

**A impressora não respondeu na porta 9100**  
Impressora desligada, IP errado, cabo desconectado ou VLAN diferente. Valide com **Testar impressora** e verifique o LED de rede da impressora.

**Validação da impressora bloqueou a instalação**  
O equipamento no IP é de outra marca/modelo ou não foi identificado. Confira o IP e o modelo selecionado.

**Gainscha imprime uma etiqueta e pula outras em branco**  
O tamanho escolhido é maior que o rolo instalado. Remova a fila no Controle de impressoras e implante novamente com o tamanho correto.

**Documentos presos na fila**  
Use **Reiniciar spooler** no Controle de impressoras.

**Usuário ou senha incorretos / domínio não encontrado**  
Confira o formato do usuário, o domínio nas Configurações e a conexão com a rede da empresa ou VPN. Se o DNS não resolver o domínio, informe um host LDAP alternativo. Fora da empresa, sem domínio, use o ícone de computador da tela de login (**Usar localmente**).

**Não foi possível conectar ao domínio (notebook de outra empresa)**  
Num computador que pertence a outro domínio, só um controlador do seu domínio consegue validar a senha, pelas portas 389 (LDAP) ou 445 (SMB). Se o DNS não encontrar o domínio, informe o IP de um controlador como host LDAP alternativo; o IP pode ser obtido numa máquina da empresa com `nltest /dsgetdc:<domínio>`. Se a porta 389 não responder (`Test-NetConnection <IP> -Port 389`), a rede ou VPN desse computador está bloqueando o acesso.

**Já existe uma conexão SMB com outra conta (Win32 1219)**  
O Windows não aceita duas sessões SMB com contas diferentes para o mesmo nome de servidor. Desde a versão 1.8.2 o Hermes tenta outro nome do mesmo destino (nome DNS, nome com ponto final ou IP) e segue com a conta informada. Se nenhum nome for aceito (por exemplo, destino informado por IP sem DNS reverso), informe o destino pelo nome ou feche as pastas dele abertas no Explorer e as unidades mapeadas (`net use \<destino> /delete`), ou faça logoff, e tente de novo.

**Uso local: a instalação falha pedindo permissão de administrador**  
Na sessão sem domínio, o Windows pede confirmação (UAC) nas etapas administrativas. Se a conta não for administradora e ninguém informar a senha de um administrador, a instalação para nessa etapa e o que foi criado é revertido.
