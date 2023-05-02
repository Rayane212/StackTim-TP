create database Stacktim
use Stacktim

Create table Utilisateurs (
idUtilisateur int identity(1,1),
codeUtilisateur varchar(50) primary key,
nomUtilisateur varchar(50), 
emailUtilisateur varchar(50),
motDePasse varchar(50),
)

create table Categorie(
	idCategorie int identity(1,1),
	codeCategorie varchar(20),
	nomCategorie varchar(20), 
	descriptifCategorie varchar(max),
	codeConnaissance varchar(20),
	codeUtilisateur varchar(max)
);

Create table Connaissance(
	idConnaissance int identity(1,1),
	codeConnaissance varchar(20),
	nomConnaissance varchar(20),
	descriptifConnaissance varchar(max),
	codeRessource varchar(20),
		codeUtilisateur varchar(max)

);

Create table Ressources(
	idRessource int identity(1,1),
	codeRessource varchar(20), 
	nomRessource varchar(20),
	datePublication DateTime,
	CreerPar varchar(50),
	descriptifRessource varchar(max), 
		codeUtilisateur varchar(max)

);

Create table TypeRessource(
	idTypeRessource int identity(1,1),
	codeTypeRessource varchar(20), 
	descriptifType varchar(max),
	image text,
	codeRessource varchar(20),

);

Create table ProjetsConnaissance(
	idProjetConnaissance int identity(1,1),
	codeProjet varchar(20), 
	codeConnaissance varchar(20)
);

Create table Projets(
	idProjet int identity(1,1),
	codeProjet varchar(20), 
	decriptifProjet varchar(max), 
	dateCreation DateTime, 
	CreerPar varchar(50),
	EtatDuProjet varchar(50),
	codeUtilisateur varchar(max)
); 



ALTER TABLE Connaissance ADD CONSTRAINT UQ_Connaissance_CodeConnaissance_CodeUtilisateur UNIQUE (codeConnaissance, codeUtilisateur);
ALTER TABLE Categorie ADD CONSTRAINT UQ_Categorie_CodeCategorie_CodeUtilisateur UNIQUE (codeCategorie, codeUtilisateur);
ALTER TABLE Ressources ADD CONSTRAINT UQ_Ressources_CodeRessource_CodeUtilisateur UNIQUE (codeRessource, codeUtilisateur);
ALTER TABLE Projets ADD CONSTRAINT UQ_Projets_CodeProjet_CodeUtilisateur UNIQUE (codeProjet, codeUtilisateur);

