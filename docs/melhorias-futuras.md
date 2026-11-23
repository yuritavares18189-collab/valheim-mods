# Melhorias futuras

Isto é o que ainda vale a pena fazer. Nada desta lista foi feito nesta documentação. Cada item espera um pedido, porque vários deles mudam texto público ou o comportamento no jogo.

## Texto

- Reescrever em português a descrição do site de Bom de Murro, Bom de Peso, Nado Rápido, Progression Task Bar e Solo Leveling. A troca para 5 reais manteve a descrição em inglês que já estava lá. A DLL do jogo não precisa mudar para isso.
- Alinhar as pastas em Documentos cujo `Plugin.cs` mostra nome em inglês. Hoje compilar essa pasta por cima do perfil troca o nome do mod na tela. O fonte em português tem que voltar a ser o arquivo que se compila. O gêmeo em inglês continua na pasta dele, separado.
- A página da Caixa do F1 1.0.0 continua no Thunderstore. O site não oferece um jeito simples de retirar o pacote. Ela não volta para o jogo.

## Herança de Atributos

- O escudo de serpente que foi melhorado no ferreiro antes da correção da cópia do `yuri.funde` precisa ser fundido de novo, dentro do jogo, com a versão atual. Primeiro os dois escudos de metal negro, depois esse resultado no de serpente.
- A janela do F7 tem tamanho no F1 e a posição continua fixa em 20, 20. Mover a janela e guardar a posição seria outra opção, se for pedida.
- O Attribute Inheritance 1.0.5 do site não recebeu o nome Eixo X e Eixo Y. Ele continua só no site, com Width e Height. Só muda se houver um pedido em inglês.

## Baú

- A 1.4.7 do site é a DLL da 1.4.6 com a frase nova. O jogo já tem a 1.4.6. Não há comportamento novo pendente nessa versão.
- As duas pastas com o mesmo GUID podem ser reduzidas a uma, com o jogo fechado, para não haver dúvida de qual arquivo carrega.

## Fartura

- A conta de vida, estamina e eitr não depende do desenho dos ícones. A 1.0.1 separa os ícones que estavam um em cima do outro.
- O CraftFree e a Cozinha Livre não mudam com este mod. Comida continua precisando ser cozinhada. Este mod só abre mais lugares para comer.

## CraftFree

- O martelo continua gastando de propósito. Liberar construção seria outro mod, e só se for pedido.
- O CraftFree não está no `mods.yml`. Funciona enquanto a pasta não for apagada. Se o gerenciador passar a controlar essa pasta, a versão do yaml tem que apontar para um zip que tenha esta DLL, senão uma extração antiga apaga o mod.

## Portal

- O clique depende da marca estar perto o bastante no chão. Se uma marca muito longe da posição do portal falhar, o ajuste é o raio, sem mudar o nome do portal e sem gravar tag.

## Fora desta leva

- A placa de vídeo RTX 3050 de dezembro continua adiada até o pedido de começar.
- `Nivel50`, `AplicaHeranca` e `CultistaUmaVez` continuam fora do site.
- Não criar pacote em inglês novo sem um pedido que peça o nome em inglês naquela mensagem.
