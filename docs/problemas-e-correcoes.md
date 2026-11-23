# Problemas, correções e o que funcionou

Este arquivo junta o que quebrou, o que foi entendido errado, o que ficou resolvido e o que já estava certo e não foi mexido.

## Herança de Atributos

O mod funde efeito de roupa e de escudo. A roupa passa o efeito para a peça que fica. Escudo com escudo: o primeiro clique some, o segundo fica, e o que fica guarda o aparar, a adrenalina de aparo e a defesa mais alta.

### Janela do F7 por cima do jogo

A janela abria sem fundo de verdade e o texto se atropelava. A correção desenha um fundo escuro opaco e calcula a altura de cada linha pelo tamanho da letra. Os botões têm altura fixa. O título é "Herança de Atributos".

### A janela aberta mandava o personagem bater

O clique do F7 chegava no ataque. O jogo não lê o clique do mouse direto nesse ponto. `PlayerController.FixedUpdate` pergunta `TakeInput`. Se a resposta é falsa, ele zera os controles e nem chega na tecla de ataque. `Player.Update` usa o `TakeInput` do personagem. Os dois métodos passam a responder falso enquanto a janela está aberta. O movimento do mouse e os botões também são zerados nesse estado. A câmera solta o cursor. Esc fecha a janela naquele quadro e não abre o menu do jogo.

### O tamanho X e Y no lugar errado

O pedido era mudar a largura e a altura da janela do F7. A primeira leitura tratou isso como mudar a janela do próprio gerenciador, que abre no F1. Nasceu a Caixa do F1, um mod separado que esticava a janela do Configuration Manager. Esse mod chegou a ser publicado e foi o caminho errado.

O certo: F1 abre o gerenciador. A pessoa clica em Herança de Atributos. Dentro desse mod existem **Eixo X** e **Eixo Y**. Eles mudam a janela do F7, não a janela do F1. Os limites são 360–1600 na largura e 360–1400 na altura. O padrão novo é 552 por 700, mas na primeira criação dessas chaves o mod lê os números antigos `Largura` e `Altura` do arquivo de config, para não jogar fora um tamanho já salvo. A janela continua no canto, em 20, 20.

A Caixa do F1 saiu da pasta de plugins. Não está no `mods.yml`. A página 1.0.0 continua no site, porque o Thunderstore não apaga um pacote já publicado. Ela não volta para o jogo.

### Melhorar no ferreiro apagava a fusão

`InventoryGui.DoCrafting` não aumenta a qualidade no mesmo objeto. Os dois caminhos de melhoria tiram o item e criam outro a partir do prefab. O mapa `m_customData` não ia junto, e a chave `yuri.funde` sumia. A partir da 1.0.11 o mapa inteiro é copiado para o item novo em qualquer peça fundida. O escudo de serpente que já tinha sido melhorado antes dessa correção perdeu a fusão. Ele precisa ser fundido de novo dentro do jogo. Os próximos melhoramentos guardam.

### Defesa, aparar e adrenalina

O bloqueio gravado é `GetBaseBlockPower`, mostrado como defesa. O campo de força é `GetDeflectionForce`. O aparar é `m_timedBlockBonus`. A parada perfeita é o nome do efeito. A adrenalina é `m_perfectBlockAdrenaline`.

O tooltip e o combate leem esses campos direto do `m_shared`. Esse objeto é o mesmo para todo item daquele tipo. O mod sobe o valor só durante a leitura e devolve o original em seguida. Não pode deixar o `m_shared` alterado. Também não pode cortar o multiplicador da habilidade de bloqueio.

O formato gravado tem 13 campos. Save antigo de 12 campos ainda abre. A adrenalina só entra se o registro tiver o décimo terceiro campo.

### O que funcionou

Com a 1.0.16 no jogo, F7 abre a janela, o personagem não anda, não olha e não bate, e Esc fecha. No F1, dentro de Herança de Atributos, Eixo X e Eixo Y mudam essa janela. Melhorar uma peça fundida daqui para a frente guarda a fusão. A subida 1.0.16 no site ficou em Valheim, Mods e Client-side, time Yuri18189.

A carta esperada depois de fundir os dois escudos de metal negro e passar o resultado para o de serpente: aparar 1,5, adrenalina 5, bloqueio pelo menos 104 e força 150, mais a resistência a perfuração que o de serpente já tem. Isso só aparece se a fusão for feita com a versão que já copia o dado.

## Rota do Baú

No jogo o nome é Rota do Baú. O pacote no site é Baurapido. O GUID é `yuri.RotaDoBau`.

Três pedidos novos:

1. **Mover Tudo**, visível só com um baú aberto. Joga o inventário nesse baú. O que não cabe vai para baús do mesmo tipo por perto, até a distância da config (no jogo está 200). Item equipado fica. Item fixado fica. A opção de só empilhar não vale para o Mover Tudo: arma, ferramenta e armadura soltas na bolsa também vão.
2. **Alt + clique esquerdo** fixa o item com a chave `yuri.fixo` e desenha uma borda verde. Guardar, Mover Tudo e Ctrl + clique esquerdo não tiram item fixado. O fixo não é limpo sozinho.
3. **Ctrl + clique direito** manda só o item clicado para o baú que está aberto, mesmo se estiver fixado. Ctrl + clique esquerdo continua a rota antiga, para um baú que já tem a pilha.

### O que atrapalhou no código

`Inventory.Changed` é privado e tem dois parâmetros. Não é chamado. A borda lê `m_elements` e `m_inventory` por reflexão, porque são privados. O botão do elemento é público. A borda são quatro faixas verdes de 3 pixels, sem bloquear o clique.

O Alt não é um modificador do jogo, então Alt + clique esquerdo chega como um clique normal. O prefixo marca o item e devolve falso, para o item não ser pego. Ctrl + clique direito no grid do jogador move a pilha para o baú aberto e devolve falso, para a comida não ser comida.

A config em português tem que continuar com as chaves antigas. Distância 200 e a lista de grupos personalizada não podem ser recriadas. Existe um bloco em inglês no mesmo arquivo, de uma DLL antiga. As chaves novas não podem usar esse bloco, senão o jogo ignora os valores em português.

O fonte em `RotaDoBau` chegou a estar em inglês e foi reescrito de volta para o português na 1.4.6. O fonte `ChestRoute` continua o pacote inglês e não pode ser compilado por cima do perfil.

### O que funcionou

A 1.4.6 está nas duas pastas de plugin. A 1.4.7 no site é a mesma DLL, com a frase de 5 reais. Categorias Valheim, Mods e Client-side.

## CraftFree

Pedido: craftar nas mesas sem ter o material.

`HaveRequirementItems` recebe o parâmetro com o nome `piece`, mesmo o tipo sendo `Recipe`. Se o patch usar outro nome, o argumento chega nulo e o mod não faz nada. O postfix só libera a receita quando o mod está ligado, a receita tem mesa, e a mesa em que o jogador está é a mesa da receita.

`DoCrafting` marca que a fabricação começou. `ConsumeResources` é chamado depois do item entrar na bolsa. Enquanto a fabricação está marcada, esse método não gasta o material, e o item craftado continua chegando.

Construir com o martelo usa outro método, `HaveRequirements` de peça. Continua pedindo material. Nível da mesa, receita conhecida e checagem de DLC continuam valendo antes da liberação do material. Melhorar um item que passa por `DoCrafting` também não gasta o material. O item em si ainda é trocado pelo caminho normal do jogo.

Cozinha Livre e Poção Livre não foram alterados por causa deste mod.

O CraftFree 1.0.0 está na pasta de plugins e no site, time Yuri18189, Valheim, Mods e Client-side.

## Portal Universal

Entrar no portal abre o mapa para escolher o destino. O mod não muda o nome do portal, não grava tag e não mexe na ligação. Quem não tem o mod continua no portal normal.

O prefixo de `TeleportWorld.Teleport` deixa o portal sem par também abrir a escolha. O clique anda pelas marcas no mapa pela distância no chão, dentro do raio da marca, com piso de 8. Se o clique não acerta a marca, o aviso é "Clique na marca do portal." A marca some quando o portal deixa de existir.

A 1.0.4 é a DLL do jogo. A 1.0.5 no site só troca a frase do donate. A lógica do clique não mudou nessa subida.

## Frase do donate

Os pacotes em português diziam "a cada 20 reais". Passaram a dizer "a cada 5 reais". Os pacotes em inglês que já existiam continuam em 4 dólares e não foram atualizados nesse pedido.

A troca foi feita no manifesto e no README, com a versão um passo acima, sem recompilar a DLL. O arquivo tem que ser UTF-8 sem BOM. O PowerShell, se gravar "UTF-8" do jeito comum, coloca BOM e o site recusa.

Funcionou nas subidas conferidas depois do sucesso: o texto novo está no pacote e as categorias ficaram Valheim, Mods e Client-side. A lista do que ainda está com a descrição em inglês está em [versões](versoes.md).

## Seis Comidas

O jogo só deixa três comidas. O limite não está num campo que alguém leia: são duas instruções fixas, uma em `Player.CanEat` e outra em `Player.EatFood`. A primeira recusa a quarta comida. A segunda, se passar, troca a comida mais gasta em vez de abrir um lugar novo. As duas passam a perguntar o número do F1. O padrão é 6, de 3 a 8.

A mesma comida já era recusada pelo nome do item. Cogumelo vermelho continua um lugar. Ensopado de cervo e ensopado de cenoura já eram itens diferentes. Não foi criada categoria.

A soma de vida, estamina e eitr já percorre a lista inteira. Não precisou de conta nova. O que faltava na tela era o desenho: o jogo só tem três ícones. O mod copia o lugar da comida e enfileira os extras no mesmo passo.

A pasta entra em plugins sem linha no `mods.yml`, para o gerenciador não extrair um zip antigo por cima. Não há pacote em inglês.

## O que já estava certo e não foi mexido

- Nado Rápido: Ctrl olha para baixo para mergulhar, olha para cima para subir, soltar o Ctrl segura a profundidade, Espaço volta à superfície, para no fundo. A velocidade sobe com a habilidade Nado.
- Progression Task Bar: com a janela aberta o jogo solta o mouse. F6 e Esc fecham. Fechada, o mouse volta para a câmera.
- Bom de Peso: os números de carga pela habilidade Corrida, com e sem o cinto.
- Bom de Murro: o combo só sobe com clique contínuo. Pausa volta no jab. No PvP o soco usa o dano real do golpe.
- Galo de Guerra: F8 chama o X1, o botão liga o PvP nos dois, no fim os dois veem o que o vencedor levou. Os dois precisam do mod.
- Cozinha Livre: receita de comida conhecida não gasta ingrediente.
- Poção Livre: base de hidromel e base de vinho de cevada conhecidas não gastam material. O fermentador continua virando a base em poção.
- Fundidor Cheio: fundidor e alto-forno não gastam carvão e aceitam 100 minérios. Carvoaria e forja de armas continuam normais.
- Monstros do Rio: a barra segue o peixe, a faixa verde é maior, não gasta isca.
- Drop Triplo: 3x do que se pega, menos o inventário da morte. O extra para quando a carga enche.
- Limpa Drop: F9 apaga o drop perto, 12 metros. Lápide e suportes ficam.
- Solo Leveling: cada ação sobe todas as habilidades, multiplicador no F1 de 1 a 5, padrão 2, sem teto.
- Flecha do jogador em 30 e flecha dos outros em 1.
- Groups e o Roses SmarterContainers continuam desligados.
- Os dois mods de baú não rodam juntos. Chest Route não entra no perfil.

## Usos únicos que não publicam

- Habilidades abaixo de 70 foram para 70 uma vez. O que já passava de 70 ficou. O fonte `Nivel50.cs` não sobe no site e não roda de novo.
- O entregador de Troféu de Cultista foi desligado porque a bolsa cheia fazia o troféu cair no chão sem parar. `CultistaUmaVez` não publica.
- `AplicaHeranca` não publica.

## Outros consertos que ficaram no jogo

- A bênção do conjunto do Lox volta na roupa junto com raiz, Fenris e furtividade. A capa do Lox só dá resistência ao gelo. A bênção é a do conjunto.
- A espada de metal negro recebe de novo o dano espiritual da espada de prata, e esse dano é reescrito enquanto o jogo está aberto para não sumir.
- A flecha dos outros estava em um décimo da velocidade. Voltou para 1. A flecha do jogador continua em 30.
- O gerenciador chegou a puxar Carry Weight, Chest Route e a barra em inglês para dentro do jogo. Os arquivos em português voltaram para o perfil.
- Os ícones chapados do site foram trocados: galo, tigela, serpente do rio, fundidor, espadas, vassoura e os três sacos. O pacote em inglês que já tinha a mesma figura recebeu a mesma imagem e não entrou no perfil.
