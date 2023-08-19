# Zadani
Aplikace s 3D vizualnim prostredim, ktera je schopna uzivateli na zaklade vstupnich dat rozjezdu umoznit namodelovat lajnu BMX skoku.

## Napady na funkcionalitu
+ V zasade se jedna o vysperkovany terrain editor s fyzikalnim modelem pod kapotou - proto bych rad zvolil pro implementaci Unity + popripade nejakou .NET UI platformu pro vytvoreni UI. Urcite bych se chtel ale drzet **jazyka C#**.
    + V aplikaci bude mozne merit vzdalenosti v metrickych jednotkach.
    + Take by stalo za zminku nejake prichytavani koncu mereni napr. ke stredum hran skoku ci rohum skoku. 
+ Kazda lajna se musi stavet na nejake mape ci podkladu. Ten se da ale pred stavenim lajny upravit pomoci klasickych funkci terrain editoru - pridat na mapu nejake vyvyseniny ci prohlubne by melo jit snadno. Tento krok by mel byt umoznen uz pred dodanim vstupnich dat rozjezdu.   
+ Po zadani vstupnich dat muze uzivatel stavet jednotlive skoky, pricemz pri konfiguraci skoku, ktera by nebyla kompatibilni se vstupnimi daty, neni umozneno tento skok postavit.
    + Zakladni stavebni jednotka je **skok**, ktery se sklada z **odrazu** a **dopadu**. Existuji ale i jine prvky, ktere mohou byt naimplementovany - boule, **klopene zatacky**, wallride, atd..
    + Na zaklade fyzikalniho modelu se po zadani parametru rozjezdu urci rychlost jezdce na zacatku lajny. Zaroven se podle uhlu, kam miri rozjezd, urci nejaky smer, kam muze jezdec po rozjezdu jet. Aplikace umozni dat nasledujici odraz ci jinou prekazku pouze v danem smeru.     
    + Pri polozeni odrazu bude nejake mensi omezeni jeho parametru na zaklade vstupni rychlosti *(napr. pokud jezdec se svou vstupni rychlosti ani nevyjede dany odraz, nemuze z nej logicky ani doskocit na dopad, tedy by aplikace nemela dovolit takovy odraz postavit)*
    + Hlavni omezeni bude ale pro polozeni dopadu. Jeho parametry budou totiz omezeny nejen vstupni rychlosti, ale i parametry jeho odrazu. Navic bude oproti odrazu vice moznosti pro jeho polozeni - zatimco odraz muze byt polozen jen tak, aby svym smerem odpovidal dosavadnimu smeru jizdy, dopad muze byt vuci svemu odrazu zrotovan az o 90&deg;. 
+ Po zadani vstupnich dat *(vcetne polohy rozjezdu na mape)* muze uzivatel na mape urcit koncovy bod, do ktereho se programem vygeneruje lajna deterministicky na zaklade nejakych parametru *(napr. obtiznost jizdy)*
    + Podobny koncept jako napr. pridani zastavky na trase v Google Maps - mozno implementovat i potahnuti za nejaky bod vygenerovane lajny, coz ma za dusledek posunuti teto lajny tak, aby vedla do daneho bodu.
    + Parametr obtiznost jizdy muze byt implementovan skrze nejake mnou predem vyrobene sablony skoku odpovidajicich jednotlivym obtiznostem, ty pak program pouzije pri generovani. Uzivatel pak uz muze ladit jen drobnejsi detaily.
+ Uzivatelske plug-ins - uzivatele si mohou vyrobit vlastni prekazky *(tezko rict, jak velkou svobodu v tomto uzivateli poskytnout, pokud by se jednalo o nejaky diametralne odlisny typ prekazky, nez bude naimplementovany defaultne, bude nutne dodefinovat napr. nejake fyzikalni vzorce popisujici jizdu po teto prekazce atd.)*
+ Moznost importovat mapu od nejakeho poskytovatele 3D map *(Google Earth)*

