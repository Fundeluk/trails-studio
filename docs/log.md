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
			- tzn. Spousta prace vybudovat vse od zakladu - **Zeptat se, zdali to ma vubec smysl!** **NEMA**
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

#### StateManager
V aplikaci jsem se rozhodl implementovat state manager, abych mohl jasne definovat mozne prechody mezi stavy. Napriklad po stavu, kde se stavi odraz dava smysl, aby mohl nasledovat jen stav, ve kterem se stavi dopad atd.
Muj statemanager je inspirovan timto:
https://gamedevbeginner.com/state-machines-in-unity-how-and-when-to-use-them/

#### 30.10.2024
##### Unity sample projekt (InputSystem_Warriors)
Pri hledani nejakych reseni mych aktualnich problemu jsem narazil na sample Unity projekt, ktery mi dal spoustu dobrych inspiraci.
Konkretne slo o:
	- Pouziti package Cinemachine, ktere velmi obohacuje moznosti prace s kamerou
	- Singleton pattern. Prevzal jsem implementaci z tohoto sample projektu do meho. Opravdu drasticky to zjednodusilo praci s nekterymi objekty v aplikaci

##### Unity GameDev skolni predmet
Zaroven jsem na predmetu, ktery je zameren na vyvoj her v Unity, narazil take na dobre rady.
Napriklad pouziti novejsiho Input System package.



##### Problemy s pouzitim Singleton patternu na UI
Snazil jsem se pouzit singleton pattern i na UI, ale zatim se mi to takto nepodarilo zprovoznit.
Prozatim to tedy necham

#### 31.10.2024
##### UIManager
Rozhodl jsem se vytvorit UIManager (skript + GameObject).
Doposud jsem udrzoval vsechny UIs v StateManageru, coz moc nedavalo smysl.

##### GridHighlighter a Singleton?
GridHighlighter je posledni dobou trochu orisek. Pri prechodu do stavu TakeOffPositionState potrebuji zobrazit dane UI, zapnout GridHighlighter a switchnout na kameru, ktera seshora trackuje highlight.
Rad bych ale, aby se vsechno tohle delo v kodu daneho stavu (v jeho metode OnEnter).
To je konkretne u enable/disable GridHighlighteru tezke, protoze stav samotny o nem nevi, a muselo by se k nemu krkolomne pristupovat pres jiny GameObject.
Zaroven udelani singletonu z GridHighlighteru je vcelku tezke rozchodit, protoze singleton rozbiji enable/disable funkcionalitu skrz gameobjekt samotny a vsechen kod, ktery na tuto funkcionalitu spoleha, po prevedeni na singleton nefunguje.

##### GridHighlighter a Eventy
Proto jsem se rozhodl zapinat GridHighlighter tak, ze pri zmene stavu v danem stavu vyvolam event, na ktery subscribne nekdo drzici referenci na GridHighlighter (UI daneho stavu) a zapne ho.
Vzhledem k informacim ze skolniho predmetu (viz. vyse) pouziji C# eventy a ne Unity eventy, protoze by to melo byt rychlejsi a budu mit moznost pouzit je ve skriptech. Jinak bych je mohl pouzit pouze v editoru.

##### Zmeny v StateManageru
Misto toho, aby si StateManager drzel jednu instanci od kazdeho stavu, se bude volat metoda ChangeState vzdy s instanci daneho stavu jako parametrem.

##### Build phase pripravena - ted uz jen vytvorit mesh odrazu podle parametru..

#### 18.11

##### Pouzit Spline package pro vytvoreni odrazu?
To nejde, spline package je urceny spise pro editovani a tvoreni splines v editoru, takze by se spatne vytvarely splines podle ciselnych parametru, coz bude primarni use case.

##### Proceduralni generovani meshe
Inspiroval jsem se timto reddit vlaknem https://www.reddit.com/r/Unity3D/comments/11nklhn/i_built_a_simple_ramp_builder_am_i_reinventing/
Tam je ve videu videt, jak jsou poskladane trojuhelniky meshe tak, aby tvorily radius.

Nejvetsi problem byl s vypocitavanim souradnic bodu na radiusu odrazu. Pouzival jsem parametricke rovnice kruznice, ktere umoznuji urcit x a y souradnice podle uhlu v radiusu. Jelikoz v realu se ale bezne stavi odrazy s danou vyskou a polomerem radiusu, musel jsem tyto uzivatelem dane promenne prepocitat na konecny uhel (pocatecni uhel je tam, kde odraz zacina, konecny je na spicce, kde se odrazi jezdec).
Ze zacatku jsem se neorientoval v tom, co v kontextu world space znamena uhel 0 stupnu, jakym smerem ten uhel pribyva a jak vlastne prepocitat zname parametry na uhel. S tim prvnim mi dost pomohly funkce Debug.DrawRay (mohl jsem si tim zobrazit, kam presne smeruji pocitane uhly) a s tim druhym kresleni vsech promennych do kruznice na papir.
Pak uz zbyvalo jen zohledneni tloustky odrazu, tedy pridani dalsich bodu a trojuhelniku, a take svah bocnic a zad odrazu, protoze odrazy z hliny nikdy nemivaji svisle steny, jen svazene. To uz vsak bylo trivialni, vzhledem k tomu, ze jsem se s generovanim meshe dost seznamil pri implementaci zakladniho radiusu.

Jelikoz jsem chtel testovat generovani meshe i v edit modu, bylo treba vytvorit custom inspector pro tento skript. Default inspector totiz sice umoznoval menit parametry, ale bylo treba zaroven vygenerovat mesh znovu kdykoliv to uzivatel chtel. Vyresil jsem to jednoduse, pridal jsem tlacitko "Redraw", ktere po stisknuti mesh vygeneruje znovu (zavola prislusnou metodu na MeshGeneratoru).

Zaroven jsem implementoval validaci inputu v tomto inspektoru, aby nebylo mozne vytvaret nesmyslne odrazy.

##### Line GameObject a ILineElement interface
Pri implementaci odrazu jsem citil potrebu zaroven trochu prekopat strukturu kodu, ktery v sobe udrzuje udaje cele lajny. Doposud si GameObject Line udrzoval kolekci LineElementu, ktere byly vsechny stejne. Kdyz jsem pak ale premyslel, jak bude faze, kde uzivatel stavi odraz interagovat s MeshGeneratorem, aby se parametry menene uzivatelem promitaly na mesh, napadlo me udelat z LineElementu takovy interface, pres ktery se budou zmeny propagovat do MeshGeneratoru. Z LineElementu se tim padem stal interface, ktery ma get a set metody pro veci jako transform, forward vector, vyska, delka, a dalsi parametry, ktere muze prekazka na lajne mit.
Pro kazdy typ prekazky na lajne se pak vytvori implementace tohoto interface napasovana primo na jeho potreby.
Napriklad u odrazu se temito settery promitnou zmeny rovnou do jeho MeshGeneratoru (na ktery si drzi referenci), a u tech parametru, kde to dava smysl, se rovnou mesh vygeneruje znovu.
	
## ROADMAP
- Implementovat highlight pres Unity Decals
- Pri urceni polohy dalsi prekazky znazornit pojezdovou plochu od minule prekazky na terenu.
- K takeoff build sliderum pridat zobrazeni jejich aktualni hodnoty
- V build phase by se krome vzdalenosti mela zobrazovat i rychlost na danem miste
- Pokud uzivatel bude chtit rozsirit lajnu do mist, kde neni teren, chci mu to umoznit

PRISTI SCHUZKA 12.12 18h