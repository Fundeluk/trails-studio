# Log
## Vyber platformy (19.8.2023)
**Kandidati:**
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