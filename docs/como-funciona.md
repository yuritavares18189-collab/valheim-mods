# Como cada mod funciona

Todos estes mods são de cliente. Cada um tem o seu GUID. Dois arquivos com o mesmo GUID não aplicam o efeito duas vezes: o BepInEx carrega um. Dois mods diferentes que mexem na mesma conta, português e inglês juntos, aplicam duas vezes. Por isso o inglês fica só no site.

A config aparece no F1, no Configuration Manager. Os mods começam recolhidos. É preciso clicar no nome do mod para ver as opções.

## Herança de Atributos

Tecla F7. A janela lista a peça e os valores que podem passar.

Roupa (elmo, peitoral, calça, capa, luva) passa o efeito. O efeito fica na peça que recebeu.

Escudo com escudo guarda, na peça que fica:

- aparar
- adrenalina de aparo
- bloqueio, se for maior
- força de defesa, se for maior
- efeito de parada perfeita, quando existe

O dado fica na chave `yuri.funde` do item, com 13 campos separados por `|`: fogo, gelo, raio, veneno, espírito, resistências, equipar, ataque, bloqueio, defesa, aparar, parada, adrenalina.

Na hora de mostrar a carta ou de bloquear, o mod lê o valor gravado. Se for maior que o da peça, usa o gravado. O objeto compartilhado do prefab é restaurado logo em seguida, porque todas as peças daquele tipo usam o mesmo objeto.

Eixo X e Eixo Y, na seção deste mod no F1, mudam a largura e a altura da janela do F7.

## Rota do Baú

Botões Guardar, Grupo, Voltar e, com o baú aberto, Mover Tudo.

Guardar manda o que pode ir para baús que já têm a pilha, respeitando a distância, o tipo, a lista de grupos e a opção de só empilhar. Item equipado e item fixado ficam.

Mover Tudo ignora a opção de só empilhar. Tenta o baú aberto. O que não cabe procura outro baú do mesmo prefab dentro da distância. Não usa a lista de pulo do Guardar e não cai num baú de outro tipo.

Alt + clique esquerdo grava `yuri.fixo` e mostra a borda verde. Ctrl + clique esquerdo num item fixado não move. Ctrl + clique direito manda aquela pilha para o baú aberto, mesmo fixada.

## CraftFree

Na mesa de craft, a receita que aquela mesa aceita e que o personagem já conhece sai sem gastar o material. A mesa errada não libera. Receita desconhecida não libera. Construir com o martelo continua gastando.

## Seis Comidas

O jogo deixa três comidas diferentes. O mod sobe esse limite. O padrão é 6. No F1, a opção Comidas vai de 3 a 8. Desligar volta para as três do jogo.

A mesma comida continua um lugar só. O jogo compara o nome do item: cogumelo vermelho não ocupa dois lugares. Ensopado de cervo e ensopado de cenoura são itens diferentes e os dois entram. Não existe uma categoria de "ensopado" que junte os dois.

A vida, a estamina e o eitr já são somados de cada comida da lista. Com seis lugares dá para juntar comida de vida com comida de mago.

Os ícones extras são cópias dos lugares que o jogo já desenha, enfileirados no mesmo passo dos três primeiros.

## Cozinha Livre

Receita de comida já conhecida não gasta ingrediente. Não libera a mesa de craft geral. Isso é o CraftFree.

## Poção Livre

Base de hidromel e base de vinho de cevada já conhecidas saem sem material. Vale para o que sai do caldeirão de hidromel. O fermentador continua transformando a base em poção.

## Portal Universal

Ao entrar no portal, o mapa abre. O clique tem que acertar a marca de um portal, pela distância no chão. As outras marcas do mapa não recebem esse clique. A marca some se o portal deixar de existir. O nome escrito no portal não muda.

## Drop Triplo

O que se pega entra em 3x, menos o inventário largado na morte. Quando a carga enche, o extra para.

## Limpa Drop

F9 apaga o drop no chão num raio de 12 metros. Lápide, suporte de item e suporte de armadura ficam.

## Nado Rápido

A velocidade na água sobe com a habilidade Nado. Segurar Ctrl e olhar para baixo mergulha. Olhar para cima sobe. Soltar o Ctrl segura aquela profundidade. Espaço volta à superfície. O mergulho para no fundo.

## Solo Leveling

Cada ação dá experiência a todas as habilidades ao mesmo tempo. O multiplicador fica no F1, de 1 a 5, padrão 2. Não há teto de nível.

## Bom de Murro

O combo da mão vazia só avança se os cliques continuam. Uma pausa volta ao jab. No PvP o soco aplica o dano real do golpe, não um décimo.

## Bom de Peso

A carga extra cresce com a habilidade Corrida. Sem o cinto, com Corrida em 50, começa em 450. Com o cinto Megingjord, 600. O bônus do cinto continua somando por cima.

## Galo de Guerra

F8 chama outro jogador para o X1. O botão liga o PvP nos dois. No fim da luta os dois veem o aviso do que o vencedor levou. Os dois jogadores precisam do mod.

## Monstros do Rio

A barra da pesca segue o peixe: segurar sobe com embalo, soltar cai e quica. Com o peixe na faixa a barra fica mais leve. A faixa verde vai de 168 até 308. O peixe pode ser misto, disparo, liso, fundo ou superfície. Não gasta isca.

## Fundidor Cheio

O fundidor e o alto-forno não gastam carvão e aceitam 100 minérios. A carvoaria e a forja de armas ficam com a regra normal do jogo.

## Progression Task Bar

A janela, os biomas e os textos ficam em português. Aberta, o mouse fica livre para a janela. F6 e Esc fecham. Fechada, o mouse volta para a câmera.

## O que o F1 é

F1 é a tecla do Configuration Manager (`com.bepis.bepinex.configurationmanager`). Ele lista os mods. Clicar no nome abre as opções daquele mod. A janela do gerenciador tem um limite de largura próprio do plugin. Nenhum mod da lista acima muda essa janela.
