## TRAILS STUDIO

*Aplikace s 3D vizualnim prostredim, ktera je schopna uzivateli na zaklade vstupnich dat rozjezdu umoznit namodelovat lajnu BMX skoku.*

### SPECIFIKACE 

#### OVERVIEW
+ Aplikace se sklada z konfiguracniho main menu (WPF) a samotneho line builderu, ktery bude implementovan pomoci WPF nebo Unity.
+ Pod line builder spada fyzikalni engine, ktery bude provadet kalkulace treni a dalsich sil pusobicich na kolo projizdejici vytvorenymi prekazkami a tim urcovat spravne parametry dalsich skoku a jinych prekazek
+ Take pod nej spada terrain editor, skrze ktery se nastavi parametry prostredi, ve kterem se budou prekazky stavet.
+ Nakonec pod nim lezi i samotny editor prekazek, ktery do prostredi umoznuje vkladat a upravovat skoky a jine prekazky.

#### USE-CASES
+ Uzivatel chce vytvorit novy spot
	+ Klikne na tlacitko "Vytvorit spot" v main menu
	+ Je tazan na parametry rozjezdu
	+ Nachazi se v terrain editoru, kde muze upravovat spot podle sebe
+ Uzivatel chce postavit skok	
	+ Klikne na tlacitko "Postavit skok" v toolboxu
	+ V terrain editoru se mu zvyrazni mista, kde je mozne dle fyzikalnich vypoctu odraz postavit
	+ Po postaveni odrazu je mozne ho dale editovat
	+ Pote se stavi dopad a rovnez jsou zvyraznena mista, kde je mozne dopad postavit
	+ Dopad se po postaveni take muze dal editovat
+ Uzivatel chce postavit jinou prekazku
	+ Uzivatel klikne na tlacitko "Postavit prekazku"
	+ A) Jednodussi varianta:
		+ Zobrazi se mu dialog, kde vybere jednu z moznych predem definovanych typu prekazek (klopena zatacka, boule, wallride, quarter pipe..)
		+ Tu umisti tam, kam mu je to dovoleno a muze ji dale editovat
	+ B) Slozitejsi varianta:
		+ Uzivatel vymodeluje prekazku kompletne podle sebe
		+ *Nejspise by vyzadovalo o dost slozitejsi integraci s fyzikalnim modelem
+ 
		
