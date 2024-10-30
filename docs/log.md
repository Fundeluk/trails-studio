# Log
## Vyber platformy (19.8.2023)
**Kandidati:**
- Evergine
	- Pro:
		- Dobra dokumentace
		- Udrzovany, drzi krok s nejnovejsim .NET
	
- Unity
	- Pro:
		- Nejrozsirenejsi a nejvic udrzovana varianta
		- Nejlepsi dokumentace
		- Pro podobny use-case jako ten muj uz bylo pouzito
	
- WPF 3D
	- https://github.com/dotnet/docs-desktop/blob/main/dotnet-desktop-guide/framework/wpf/graphics-multimedia/3-d-graphics-overview.md
	- Pro:
		- Velice dobra dokumentace od Microsoftu
	- Proti:
		- Basic funkcionalita
			- tzn. Spousta prace vybudovat vse od zakladu - **Zeptat se, zda-li to ma vubec smysl!**
		- napr. Helix toolkit je vlastne rozsireni tohoto
		- Pro slozitejsi vizualizace je pomalejsi

- Helix Toolkit (VYRAZEN)
	https://github.com/helix-toolkit/helix-toolkit
  - Pro:
    - Nadstandardtni funkcionalita oproti defaultnimu WPF 3D
	- Examples repo ukazujici funkcionalitu
  - Proti:
    - V podstate neexistujici dokumentace
	- Cely tento framework vypada ze nedrzi krok s aktualnim C#
	- Neni moc udrzovany

- Monogame (VYRAZEN)
	- Pro:
		- Dobra dokumentace
		- Hodne tutorialu
	- Proti:
		- Nejspis nebylo pouzito pro muj use-case
		- Vice low-level nez unity

- Silk.NET (VYRAZEN)
	- OpenGL API pro C#
	- https://dotnet.github.io/Silk.NET/docs/
	- Pro:
		- Dobra dokumentace
		- Rychly
	-Proti:
		- Hodne low-level
		- Tezke vubec rozbehnout
		- Moc komplikovana knihovna pro muj use-case
	
- Veldrid (VYRAZEN)
	- To stejne co Silk .NET (jen vice podporuje multiplatforming)

- Blotch3D (VYRAZEN)
	https://github.com/Blotch3D/Blotch3D
	- Pro:
		- Funkcni examples ALE jen nektere + outdated (pro verzi NET Core 3.0)
	- Malo udrzovany maly projekt bez dokumentace
	
	
## Vybrany framework: Evergine

## ZMENA
Po nekolika dnech implementovani v Evergine jsem nasel spoustu problemu, ktere mi brani v pokracovani. (viz. Problemy#Evergine)
Proto jsem se rozhodl zmenit framework a pokracovat budu v Unity.

## Vybrany framework: UNITY

## Problemy
##### EVERGINE
- Ackoliv je spousta samplu ve WPF variante, bez jakekoliv zminky v dokumentaci to vypada, ze uz tuto moznost aktualni verze nenabizi
- Kamera vytvorena v kodu
	- Zobrazi vytvorene entity (floor, rozjezd) pouze pri pridani skrze Evergine Studio, nebo pokud se prida v kodu s implementovanym CameraBehavior.cs (zatim okopirovany z EverSneaks dema)
	- Nereaguje na vstup z klavesnice
	- FIX: https://github.com/EvergineTeam/Feedback/issues/192
- Po kazdem spusteni dojde v radu desitek vterin ke spadnuti cele aplikace, a to i pokud je naimplementovana jednoducha vec, ktera nema duvod.
- kamera se stale spawnuje na stejnem miste (origin)
	- mozna zpusobeno tim, ze local i world position maji v tomto pripade origin ve stredu flooru.
	- EDIT: ano, to s originem je pravda, ale tento problem se vyskytuje i tak.
- lookAt metody kamery (jejiho transform komponentu) nic nedelaji.
- mozne reseni: projit sample od tvurcu Evergine (konkretne SmartCity), kde se vyskytuji podobne prvky, jako v me aplikaci
- I po snaze vyresit problem inspiraci od poskytnutych samplu se Evergine stale chova nepredvidatelne
	- Napr. kamera se pri spusteni z Evergine Studia (ES) chova, jak ma (da se posouvat klavesami). Pri spusteni z Visual Studia (VS) toto ale nefunguje.
	- Dale se pri vytvoreni Entities skrze ES da aplikace spustit, obema zpusoby. Ale pokud se Entity vytvori skriptem v kodu (ale jsou identicke s tou ES verzi), aplikace pri jejich tvoreni spadne na nezname vyjimce.
- Obecne se Evergine chova nekonzistentne mezi implementaci tech samych veci v ES a VS, v nekterych pripadech v jedne variante vubec nereaguje na zmeny.

##### UNITY
- Obcas je tezke nastavit rotaci objektu tak, jak si to predstavuji
	- Napriklad text znazornujici vzdalenost mezi pozici nove prekazky od posledni postavene v build phase:
	- Chtel jsem, aby ten text byl umisten uprostred a podel linie spojujici tyto dve prekazky, a lezel na terenu
	- To trvalo netrivialni dobu vymyslet, a nakonec jsem se musel uchylit ke kombinaci prirazeni smerovaciho vektoru (transform.right) a otoceni kolem jedne osy "natvrdo" o dany pocet stupnu

#### 30.10.2024
Pri hledani nejakych reseni mych aktualnich problemu jsem narazil na sample Unity projekt, ktery mi dal spoustu dobrych inspiraci.
Konkretne slo o:
	- Pouziti package Cinemachine, ktere velmi obohacuje moznosti prace s kamerou
	- Singleton pattern. Prevzal jsem implementaci z tohoto sample projektu do meho. Opravdu drasticky to zjednodusilo praci s nekterymi objekty v aplikaci

Zaroven jsem na predmetu, ktery je zameren na vyvoj her v Unity, narazil take na dobre rady.
Napriklad pouziti novejsiho Input System package.
	

	
## ROADMAP
- Implementovat highlight pres Unity Decals
- Mozna udelat dva ruzne zpusoby vytvoreni spotu:
	1. zadani vysky a uhlu rozjezdu (TEMER HOTOVO)
	2. zadani vstupni rychlosti
- V build phase by se krome vzdalenosti mela zobrazovat i rychlost na danem miste
- Pokud uzivatel bude chtit rozsirit lajnu do mist, kde neni teren, chci mu to umoznit