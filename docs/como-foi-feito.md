# Como foi feito

## Código

Os mods são DLL de BepInEx, em C# 5, compiladas com o `csc` do .NET Framework 4 em `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. O código não usa sintaxe mais nova que o C# 5.

A compilação referencia o `netstandard.dll` do jogo, o `BepInEx.dll`, o `0Harmony.dll` e as DLLs de `valheim_Data\Managed` que o fonte realmente usa. Atalho de campo privado do Harmony (`___campo`) é usado quando o campo não é público. Não se publica um arquivo falso no lugar da DLL do jogo.

Mod com janela também referencia `UnityEngine.IMGUIModule.dll`. Texto da interface pode precisar de `UnityEngine.TextRenderingModule.dll`. Clique e tecla do jogo passam por `assembly_utils.dll` (`ZInput`, `ZCursor`). `Input.GetKey` precisa de `UnityEngine.InputLegacyModule.dll`. A Herança 1.0.16 falhou a primeira compilação sem essa referência e a DLL antiga ficou no disco até a segunda compilação, que saiu com 31232 bytes.

No PowerShell, a lista de argumentos do compilador não pode se chamar `$args`, porque o PowerShell esvazia esse nome. Caminho com espaço, como a pasta do Thunderstore Mod Manager, vai entre aspas dentro do `/reference`.

O patch do Harmony tem que usar o nome do parâmetro que o método original usa. Em `HaveRequirementItems` o nome é `piece`, embora o tipo seja `Recipe`.

## Onde o jogo lê o mod

Perfil:

`C:\Users\Yuri\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Default`

A DLL carregada fica em `BepInEx\plugins`. O `mods.yml` diz qual pacote o gerenciador considera instalado. Se a versão do yaml muda para uma que o catálogo conhece, o gerenciador extrai de novo o zip em cache e passa por cima de uma DLL copiada à mão. Por isso uma DLL nova às vezes entra na pasta de plugins e também dentro da pasta de cache da versão que o yaml já aponta, sem mudar o número no yaml.

Não se copia uma DLL com o Valheim aberto. O arquivo mapeado não deixa gravar.

Pasta extra sem linha no `mods.yml` não é reextraída. Portal Universal, CraftFree e Seis Comidas estão assim. Isso não vale para apagar pastas que já estão no jogo, com uma exceção: a Caixa do F1 foi removida porque o pedido foi tirá-la.

Seis Comidas troca as duas instruções fixas do número 3, em `CanEat` e em `EatFood`, por uma chamada ao limite do F1. A tela da comida usa `UnityEngine.UI.dll`, `UnityEngine.UIModule.dll` e `Unity.TextMeshPro.dll`.

## Site

Cada pacote é um zip com quatro arquivos na raiz: `manifest.json`, `README.md`, `icon.png` e a DLL. O manifesto e o README são UTF-8 sem BOM. A descrição do manifesto tem no máximo 250 caracteres.

Time Yuri18189. Comunidade Valheim. Categorias Mods e Client-side. A dependência declarada é `denikson-BepInExPack_Valheim-5.4.2350`. O site do donate no manifesto é https://ko-fi.com/yuritavares60974

A frase do pacote em português termina pedindo a ideia a cada 5 reais, escrita na mensagem do donate. Não entra e-mail nesse texto.

Quando a mudança é só essa frase, a DLL não é recompilada. A versão sobe um passo, o zip novo sobe no site, e o perfil do jogo não é apontado para essa versão, para o gerenciador não trocar a DLL que já está certa.

O pacote em inglês só é criado ou atualizado quando o pedido daquela mensagem pede o nome em inglês. Atualizar o português não atualiza o gêmeo em inglês sozinho.

## O que a compilação não faz

Não edita o arquivo do personagem. Não usa comando de dar item. Não muda mundo. Não mexe na velocidade da flecha. Não reinstala a Caixa do F1. Não liga de novo o Groups nem o Roses SmarterContainers.
