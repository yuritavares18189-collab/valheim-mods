# Versões

Três lugares diferentes, de propósito.

| Lugar | O que é |
| --- | --- |
| Pasta em Documentos | O `Plugin.cs` que está na máquina. Algumas pastas foram reescritas com o nome em inglês na tela. Compilar essa pasta por cima do perfil troca o nome do mod no jogo. |
| Jogo | A DLL que o BepInEx carrega no perfil Default. O gerenciador só reextrai o pacote se a versão do `mods.yml` mudar. Por isso a versão do yaml não sobe quando a DLL nova foi copiada à mão. |
| Site | O pacote no Thunderstore. Várias versões novas só trocaram a frase do donate de "20 reais" para "5 reais". A DLL de dentro é a mesma da versão anterior. |

## Português no jogo e no site

| Mod no jogo | GUID | Fonte local | Versão no fonte | No site agora |
| --- | --- | --- | --- | --- |
| Herança de Atributos | yuri.HerancaDeAtributos | FundeAtributo | 1.0.16 | 1.0.16 |
| Rota do Baú (pacote Baurapido) | yuri.RotaDoBau | RotaDoBau | 1.4.6 | 1.4.7, texto |
| CraftFree | yuri.CraftFree | CraftFree | 1.0.0 | 1.0.0 |
| Seis Comidas | yuri.SeisComidas | SeisComidas | 1.0.0 | 1.0.0 |
| Portal Universal | yuri.PortalUniversal | PortalUniversal | 1.0.4 | 1.0.5, texto |
| Monstros do Rio | yuri.MonstrosDoRio | MonstrosDoRio | 1.0.14 | 1.0.15, texto |
| Poção Livre | yuri.PocaoLivre | PocaoLivre | 1.0.4 | 1.0.5, texto |
| Drop Triplo | yuri.DropTriplo | DropTriplo | 1.0.7, nome de tela em inglês | 1.0.11, texto |
| Limpa Drop | yuri.LimpaDrop | LimpaDrop | 1.0.1, nome de tela em inglês | 1.0.5, texto |
| Nado Rápido | yuri.NadoRapido | NadoFundo | 1.0.2, nome de tela em inglês | 1.0.5, texto |
| Solo Leveling | yuri.SoloLeveling | SoloLeveling | 1.0.1 | 1.0.4, texto |
| Bom de Murro | yuri.BomDeMurro | PunhoLivre | 1.0.9, nome de tela em inglês | 1.0.12, texto |
| Galo de Guerra | yuri.GaloDeGuerra | GaloDeGuerra | 1.0.3, nome de tela em inglês | 1.0.8, texto |
| Fundidor Cheio | yuri.FundidorCheio | FundidorCheio | 1.0.2, nome de tela em inglês | 1.0.6, texto |
| Cozinha Livre | yuri.CozinhaLivre | CozinhaLivre | 1.0.1, nome de tela em inglês | 1.0.5, texto |
| Bom de Peso | yuri.BomDePeso | BomDePeso | 1.0.1, nome de tela em inglês | 1.0.4, texto |
| Progression Task Bar | yuri.ProgressionTaskBar | ProgressionTaskBar | 1.0.6 | 1.0.9, texto |

"Texto" quer dizer que a versão do site subiu, a frase do donate passou a 5 reais, e a DLL publicada é a da versão anterior.

O Herança no yaml do gerenciador continua apontando para 1.0.15. A DLL dessa pasta de cache foi trocada pela 1.0.16, para o gerenciador não voltar o arquivo antigo na próxima extração. O CraftFree está na pasta de plugins sem linha no `mods.yml`, pelo mesmo motivo. Seis Comidas também: a pasta é `Yuri18189-SeisComidas` e não tem linha no yaml.

A Rota do Baú tem duas pastas de plugin com o mesmo GUID: `Yuri18189-Baurapido` e `Yuri-RotaDoBau`. As duas estão com a DLL de 1.4.6. O BepInEx carrega um GUID uma vez. O pacote antigo `Baurapido-Baurapido` fica desligado.

## Inglês só no site

| Pacote | GUID no fonte | Fonte local |
| --- | --- | --- |
| Attribute Inheritance | yuri.AttributeInheritance | AttributeInheritance 1.0.5 |
| Chest Route | yuri.ChestRoute | ChestRoute |
| Triple Drop | yuri.TripleDrop | TripleDrop |
| Clear Drops | yuri.ClearDrops | ClearDrops |
| Quick Swim | yuri.QuickSwim | QuickSwim |
| Full Smelter | yuri.FullSmelter | FullSmelter |
| River Monsters | yuri.RiverMonsters | RiverMonsters |
| Free Cooking | yuri.FreeCooking | FreeCooking |
| Free Potions | yuri.FreePotions | FreePotions |
| War Rooster | yuri.WarRooster | WarRooster |
| Punch Combo | yuri.PunchCombo | PunchCombo |
| Carry Weight | yuri.CarryWeight | CarryWeight |
| Story Bar | yuri.StoryBar | StoryBar |
| Rise Together | yuri.RiseTogether | RiseTogether |

Portal Universal não tem pacote em inglês. CraftFree não tem um segundo pacote.

## Descrições do site que ficaram em inglês

A troca para "5 reais" copiou a descrição que já estava no site e só mudou o valor. Estas descrições continuam em inglês, com a frase nova do donate em português no final:

- Bom de Murro 1.0.12
- Bom de Peso 1.0.4
- Nado Rápido 1.0.5
- Progression Task Bar 1.0.9
- Solo Leveling 1.0.4

O nome dentro do jogo desses mods não mudou com essa subida, porque a DLL do perfil não foi trocada. A correção da frase no site desses cinco ainda está em aberto. Está em [melhorias futuras](melhorias-futuras.md).
