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

##### Proceduralni generovani meshe odrazu
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

##### Proc. generovani meshe pro dopad
Puvodne jsem myslel, ze predelat generovani meshe pro dopad bude v podstate jen prekopirovani verze pro odraz.
Bohuzel to tak nakonec ale nebylo.
Dopad ma jine vstupni parametry, od odrazu se lisi tim, ze misto radiusu ma sklon.
Pokud by mel dopad pouze svazenou dopadovou plochu, mel by zlom v miste, kde se potkava se zemi.
Proto jsem se rozhodl, ze od pulky jeho vysky bude vest k zemi radius, ktery plynule navaze na rovinu podkladu.
Nebylo ale jednoduche vymyslet, jak vykreslit tento radius, pokud vim pouze uhel sklonu dopadove plochy a vysku, ve ktere je prechod mezi rovinou dopadove plochy a radiusem.
Pote mi take doslo, ze pro vykreslovani dopadu se oproti odrazu zmeni vnimani poloh nekterych vrcholu.
Napr. cim dale jsem na odrazu, tim prudci a vyssi je radius. U dopadu je to ale naopak.
Nakonec se mi povedlo naimplementovat generovani dopadu, az kdyz jsem si vyjadril polomer jeho kruznice pomoci delky vektoru vedoucich z pocatecniho a koncoveho bodu do stredu kruznice. Vedel jsem totiz obe souradnice u toho koncoveho (tam, kde se dotyka tecna), a y-souradnici toho pocatecniho (0). Zaroven jsem mohl urcit smer vektoru, protoze z koncoveho bodu to byl kolmy na tecnu, a z pocatecniho to byl primo vzhuru.
Se ziskanym polomerem uz bylo mozne vyjadrit i vse ostatni, a pak jsem byl schopen dosahnout vykreslovani tak, jak jsem si ho predstavoval, ale obcas jsem byl nucen zkouset resit problemy metodou pokus-omyl a nakonec mi jen toto trvalo nekolik dni. 

Co bych ale mel jeste dodelat, je spojeni spolecne funkcionality mesh generatoru odrazu i dopadu do jedne tridy.

##### Topdown camera troubles
Po vytvoreni podpory pro staveni dopadu jsem narazil na mensi trable s kamerou. Jelikoz totiz jde dopad vuci odrazu postavit ruznymi uhly (zatimco odraz jde stavet jen pod uhlem, ve kterem se k nemu prijizdi), chtel jsem aby topdown kamera, ktera je aktivni, kdyz uzivatel urcuje pozici budouciho dopadu, byla otacena podle uzivatelem navrhovaneho uhlu.
Tam jsem se ale setkal s ruznymi prekazkami, napriklad ze ackoliv ma highlight objekt renderovanou pouze svoji predni stranu, kdyz jeho forward vector miri nahoru, je ta renderovana strana smerem dolu. Musi tedy byt otocen opacnym smerem, nez se na prvni pohled zda.
Tim padem pokud ma topdown kamera jako target takto otoceny highlight, klasicky offset udavajici jeho vysku nad nim je vniman tak, ze je kamera pod mapou a kouka na target "zespodu".
Diky teto zvlastni orientaci os highlightu se take zvlastne chovalo jeho nataceni podle uhlu dopadu, takze jsem musel jeho rotaci nastavovat zvlastnim zpusobem (volat metodu typu LookRotation s upwards parametrem jinym, nez je doopravdy smer odpovidajici "upwards").
Musel jsem experimentovat s ruznym chovanim ovladani pozice a rotace kamery, aby to odpovidalo mym predstavam. Sice to slo spise metodou pokus omyl, ale nakonec musim rict, ze to slo docela dobre, protoze cinemachine ma v sobe nativne zabudovanou spoustu ruznych chovani, ktere se daji v inspektoru dobre nastavovat i za behu.

##### Detail camera troubles
Dalsi kameru, kterou jsem chtel zmenit, byla sideview/detailni kamera, ktera je aktivni, kdyz se nastavuji parametry odrazu/dopadu. Do teto chvile tato kamera mela danou fixni pozici a uhel vuci kazde prekazce.
To bylo ale neobratne. V nekterych use-casech davalo smysl, aby si uzivatel nastavil uhel pohledu kamery sam podle toho, jaky parametr zrovna nastavoval.
Nastesti i pro tento use-case ma Cinemachine plugin podporu - Orbital follow pro omezeni pozice kamery do koule kolem targetu a rotation composer pro zamireni kamery na target. Orbital follow ma rovnou i podporu pro nejaky input, kterym se pak kamera ovlada. Tam se da dosadit jakakoliv Action z Input Systemu. Pro click&drag ale Input System defaultne nema action, musel jsem ji vytvorit sam. To bylo nakonec jednoduche, ale v praxi to obcasne vyhazuje error, ackoliv to jinak funguje bez problemu. (pozn. dalsi den to error nahodne vyhazovat prestalo.)
Zaroven Orbital follow podporuje zmenu polomeru koule (tedy zoom). Kdyz jsem tam ale dosadil defaultni scroll action z input systemu, nefungovalo to.
Po lehkem googlovani jsem ale nalezl reseni zde: https://discussions.unity.com/t/cinemachineinputaxiscontroller-orbitscale-inputsystem-not-work/1569913

##### Building UI
Pri nastavovani parametru dopadu mi vyhazuje console hlasky typu "Recursively dispatching event from another event", pricemz vzdy jde o nejaky UI prvek v Landing Build UI.
Rozhodl jsem se tedy cele UI pro vystavbu skoku prepracovat.

##### Mazani prekazek
TODO: dopsat prubeh implementace mazani prekazek, zminit problem s propagaci click eventu na tlacitko dal i na jeho podklad.

##### Editace terenu
Tohle trvalo dost dlouho vubec vymyslet. V idealnim pripade by se editoval teren vsude pomoci brushe a pote by si aplikace sama spocitala pomoci normal terenu zmeny v rychlosti v zavislosti na jeho tvaru. To mi ale prislo jako nadlidsky ukol, protoze takovyto vypocet by bylo hrozne slozite vymyslet i zrealizovat a zaroven by byl nejspis dost narocny na hardware.
Proto jsem se rozhodl nechat uzivatele zvolit v jakemkoliv miste za posledni aktualne postavenou prekazkou bod, kde chce zacit se zmenou terenu (coz je proste bud sklon z kopce nebo do kopce), dale pak zvolit konecny bod, a nakonec urcit vyskovy rozdil mezi temito dvema body.
Po urceni techto tri veci se ve vsech mistech krome tech, kde uz vede lajna nebo stoji nejaka prekazka zmeni vyska terenu tak, aby odpovidala te, kterou urcil uzivatel na konci sklonu.

Pri implementaci byla prvni prekazka rozmyslet si, jak pristupovat k te zmene vysky terenu ve vsech mistech krome stavajici lajny. Pro ucely teto celkove zmeny terenu by bylo nejlepsi si drzet kopii heightmapy, kde by pro kazdou souradnici byl napr. bool udavajici, zda-li na tomto miste je lajna (a tedy se v tomto miste vyska terenu menit dale nema), nebo jestli tam lajna neni (a tim padem se v tomto miste vyska terenu zmenit ma).
Pokud jsem na to nahlizel z pohledu jednotlivych prekazek, ktere uzivatel muze ruzne pridavat i mazat, tak mi to predchozi reseni neprislo uplne vhodne. Napr. pokud by se nejaka prekazka smazala, mely by se tim padem zmenit i udaje v kopii heightmapy. Z toho logicky plyne, ze seznam souradnic na heightmape, ktere zabira prekazka, by se mel udrzovat v prekazce samotne, kde se da rozeznat, jake souradnice patri jake prekazce, nez aby byly nahazene dohromady v te kopii.
V pripade souradnic udrzovanych v jednotlivych prekazkach ale pak vznika problem pri te celkove zmene terenu, protoze tam se nabizi prochazet heightmapu iteraci 2d indexu od zacatku do konce, kde bych potom musel v kazde iteraci projit vsechny prekazky a ptat se, jestli se na dane souradnici na heightmape nenachazeji. To mi nezni moc chytre ani efektivne.
Zatim jsem se rozhodl pro prekazky vypocitat souradnice na heightmape z jejich Bounds objektu jejich Meshu.

Ziskani Bounds objektu nebylo uplne primocare - u odrazu a dopadu se nabizi pouzit Bounds jejich collideru, pripadne meshe. Tady jsem ale zjistil, ze ani jeden z takto ziskanych Bounds neni pouzitelny - V pripade meshe je cely vynulovan, v pripade collideru ma spravne nastavenou pozici, ale jeho size je nulovy na vsech osach.
Musel jsem tim padem Bounds vytvorit manualne, coz vsak nebyl velky problem, protoze ma metodu Encapsulate, ktera ho rozsiri tak, aby obsahoval souradnice poskytnute pri volani funkce.

Pro reprezentaci plochy prekazky staci jen ctyri cisla: zacatek na X ose, zacatek na Z ose a delky na obou osach (kolik souradnic prekazka zabira od start bodu na dane ose).
Tyto souradnice (souradnice vsech rohu Bounds) pote prelozim na souradnice heightmapy, coz neni uplne primocare, tady jsem se inspiroval temito zdroji:
https://www.reddit.com/r/Unity3D/comments/2vf70k/referencing_terrain_points_via_world_position/
https://discussions.unity.com/t/terrain-leveling/798993
https://discussions.unity.com/t/terrain-cutout/563953
https://github.com/kurtdekker/makegeo/tree/fbec609c855311f6f102aff16f24ff26c6db76f3/makegeo/Assets/TerrainStuff.

Pri dalsi implementaci jsem si uvedomil, ze prekazky samotne si udaje tykajici se zmeny terenu nemusi udrzovat. Prekazka samotna teren nemeni, teren pod ni je urcen zmenou terenu pred ni, tedy tyto udaje staci mit ulozene v objektu zmeny terenu (SlopeModifier).

Jakmile jsem ale dokoncil fungujici implementaci podle dosavadniho planu, zjistil jsem, ze to bude chtit nejake zmeny.
Pokud ma SlopeModifier dany smer, uzivatel na nej polozi odraz a pak chce polozit dopad v jinem smeru, nez je ten dany SlopeModifierem, dopad polozi na koncovou vysku SlopeModifier.
Dopad ale muze chtit polozit na vysku SlopeModifier podle vzdalenosti od odrazu.
Tedy si implementace vyzaduje, aby byl SlopeModifier urceny jen tim, kde ma zacinat, jak ma byt dlouhy, a jaky ma mit vyskovy rozdil. Jeho konecna poloha a cesta kudy vede jsou dany tim, jake uzivatel postavi prekazky na jeho rozsahu a kde tyto prekazky umisti.

Po prekopani implementace SlopeChange jsem zjistil, ze dosavadni postup pri staveni prekazek nebude s temito zmenami terenu fungovat.
Vypada to, ze unity pozna zmenu sklonu terenu, a pokud se na ni instancuje objekt, automaticky nastavi jeho rotaci tak, aby sklon kopiroval. To je zadouci chovani. Bohuzel ale pri dosavadnim postupu vznika odraz (coz implicitne znamena, ze se i vykresli jeho mesh) pred zmenou terenu do jeho koncoveho bodu, takze se vykresli s predchozim sklonem a nekopiruje tu novou zmenu. Toto poradi stavby je ale tak hluboce zakorenene v kodu, ze pri rozmysleni jak to zmenit mi doslo, ze vzhledem k budouci implementaci fyzikalniho modelu je zadouci, aby kod pro staveni prekazek pouzival builder pattern a kompletne se zmenil zpusob, jak se s nim bude v jednotlivych fazich stavby zachazet.
Nejdrive vznikne builder odrazu, ktery ho uz vykresluje, ale s materialem znazornujicim, ze zatim odraz jeste neni dokoncen. Na builderovi budou zpristupneny settery parametru, pomoci kterych se bude prekreslovat. Mel by vlastne nahradit i highlight ve fazi umistovani, kdy se misto zvyrazneneho ctverce na terenu bude rovnou vykreslovat tvar odrazu (a v budoucnu mozna dle fyzikalniho modelu vhodne menit podle pozice). V tuto chvili ale stale nijak neinteraguje s instanci Line, protoze do dostaveni neni jeji soucasti.
Po potvrzeni finalnich parametru teprve vznikne instance tridy Takeoff, mesh odrazu se vykresli s materialem hliny a prida se do seznamu prekazek v Line.
Toto chci zmenit i pro ostatni prekazky.

Zaroven jsem chtel trochu vycistit kod staveni odrazu. Hlavne to, ze kod pro generovani meshe (trida TakeoffMeshBuilder) je vlastne implementacni detail, a nemel by byt viditelny pro jakekoliv tridy krome tech, ktere se odrazu tykaji (tedy jeho builderu a tridy Takeoff). To se ukazalo byt jako docela orisek, protoze jak TakeoffMeshGenerator, tak i ty dve ostatni musi dedit od zakladni Unity tridy MonoBehaviour, protoze potrebuji mit pristup k Unity rezijnim metodam (kresleni meshe, tvorba gameobjektu jako cil pro kameru atd. a jejich niceni).
Pokud je ale trida MonoBehaviour, musi byt v samostatnem zdrojaku. Tady prisla kolize s tim, ze ma byt mesh generator viditelny jen pro ty dve tridy. Pouze public/private pristupnost nestaci, protoze vlastnosti/metody jsou bud pristupne vsem, nebo nikomu. Vnorit tyto dve tridy do mesh generatoru by neslo, protoze musi byt tridy ve svych zdrojacich a navic by to byly public tridy v private tride, coz vlastne neni mozne.
Zaroven kvuli rozdeleni do samostatnych zdrojaku nelze pouzit internal modifier.
Nakonec jsem se tento zadrhel rozhodnul obejit compiler atributem "assembly: InternalsVisibleTo", ktery umozni zpristupnit polozky s pristupnosti internal pro vyjmenovane zdrojaky. To by tento problem melo vyresit.
Po vyzkouseni tohoto postupu se ale ukazalo, ze ani toto fungovat nebude, protoze aby se mohla predat instance mesh builderu z takeoff builderu do takeoffu, musi byt jako parametr v public inicializacni metode, coz se neprelozi, protoze tam mesh builder jakozto internal trida byt nemuze.
Nakonec jsem tedy udelal mesh builder public.

###### Klopene zatacky?
Rozhodl jsem se neimplementovat klopene zatacky, protoze se na realnych tratich nevyskytuji tak casto jako normalni skoky a tim padem nejsou nutna feature v aplikaci. Daji se nahradit skokem do strany. Muze to byt vhodne rozsireni pro praci.

###### Mouseover manager
Kvuli implementaci tooltipu u prekazek jsem se rozhodl udelat sjednoceni handlingu mouse eventu v podobe mouseover managera. S temito eventy se totiz uz pracuje i v Delete funkci.
Manager ale bude spravovat tyto eventy, na ktere se bude moci zavesit vice subscriberu. Napriklad delete UI vyuziva mouseover, onclick i mouseexit event. Tooltip funkcionalita bude pouzivat hlavne onclick event (ale mozna i mouseover pro zvyrazneni mouseover prekazky).
Manager ma pridanou hodnotu v tom, ze spravuje vsechny tyto eventy dohromady, takze v pripade, ze se zadne eventy neodebiraji, nemusi vubec provadet raycast v kazdem volani Update, zatimco kdyby tyto funkce byly rozdelene do vlastnich skriptu a jejich Update metod, tak by se tento raycast provadel dokonce nekolikrat za jeden Update cyklus.
Hlidani odebiranych eventu jsem vyresil skrze Event properties a slovnik s hash kodem delegatu jako klicem a delegatem jako hodnotou, ktery slouzi jako backing field pro kazdy event. Tim padem mam pristup k metodam pro pridani/odebirani subscriberu u eventu a muzu si udrzovat citac celkoveho poctu subscriberu. Pokud je citac nulovy, nemusim v Update metode cokoliv resit.

## ROADMAP

- V default view umoznit kliknuti na prekazky a tim zobrazit jejich parametry
- Toggle v default view pro zobrazeni informaci o slope changes - zvyraznit pocatecni/koncovy bod, zobrazit vyskovy rozdil a delku
- measure funkce
- V build phase by se krome vzdalenosti mela zobrazovat i rychlost na danem miste
