create database Stacktim
use Stacktim

create table Categorie(
	idCategorie int identity(1,1),
	codeCategorie varchar(20) primary key,
	nomCategorie varchar(20), 
	descriptifCategorie varchar(max),
	codeConnaissance varchar(20)

);

Create table Connaissance(
	idConnaissance int identity(1,1),
	codeConnaissance varchar(20) primary key,
	nomConnaissance varchar(20),
	descriptifConnaissance varchar(max),
	codeRessource varchar(20)
);

Create table Ressources(
	idRessource int identity(1,1),
	codeRessource varchar(20) primary key, 
	nomRessource varchar(20),
	datePublication DateTime,
	CreerPar varchar(50),
	descriptifRessource varchar(max), 
);

Create table TypeRessource(
	idTypeRessource int identity(1,1),
	codeTypeRessource varchar(20) primary key, 
	descriptifType varchar(max),
	image text,
	codeRessource varchar(20)
);

Create table ProjetsConnaissance(
	idProjetConnaissance int identity(1,1),
	codeProjet varchar(20), 
	codeConnaissance varchar(20)
);

Create table Projets(
	idProjet int identity(1,1),
	codeProjet varchar(20) primary key, 
	decriptifProjet varchar(max), 
	dateCreation DateTime, 
	CreerPar varchar(50),
	EtatDuProjet varchar(50)
); 
