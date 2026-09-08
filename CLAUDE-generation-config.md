# Le générateur de configurations

Comment les `.vdf` sont produits : le problème de la taille et des répétitions, les refs VDF,
le passage de paramètres à l'inclusion, les variables propres à un contrôleur, les helpers
handlebars, et la génération de plusieurs contrôleurs.

> Fichier de contexte de **KSP-SteamInputPlugin**. Vue d'ensemble dans [CLAUDE.md](CLAUDE.md).


## Le problème

Un fichier correspondant à un controlleur est potentiellement très gros, surtout quand on doit gérer de nombreux presets, avec des nombreux bindings.

De plus, il y aura beaucoup de répétitions à l'intérieur car un même groupe pourra être utilisé à plusieurs endroits, et c'est la même chose pour les inputs et les bindings.

Et l'objectif étant de proposer des configurations pour plusieurs manettes, il y aura beaucoup de répétitions entre ces configurations.

Pour modulariser tout cela, ce dépot contient un script node qui assemble des configurations de controlleurs à partir d'un ensemble de fichiers.

## L'implémentation des refs VDF

Le script repose sur une implémentation qui ressemble à ce que l'on fait avec des JSONRef, mais sur des fichiers VDF.

Voici un exemple de fichier racine :

    "MonObjet"
    {
        "propriete"     "valeur"
        "sousObjet"
        {
            "propriete"     "valeur"
        }
        "#ref"      "sousfichier1.vdf"
        "#ref"      "sousfichier2.vdf"
    }

Et voici le code du 1er sous fichier nommé "sousfichier1.vdf" : La propriété racine "ref" est obligatoire

    "ref"
    {
        "sousObjet1"
        {
            "propriete1"    "valeur1"
        }
    }

Et le code du 2eme sous fichier nommé "sousfichier2.vdf" : La propriété racine "ref" est obligatoire

    "ref"
    {
        "sousObjet2"
        {
            "propriete2"    "valeur2"
        }
    }

Le résultat de la fusion sera :

    "MonObjet"
    {
        "propriete"     "valeur"
        "sousObjet"
        {
            "propriete"     "valeur"
        }
        "sousObjet1"
        {
            "propriete1"    "valeur1"
        }
        "sousObjet2"
        {
            "propriete2"    "valeur2"
        }
    }

Le fonctionnement n'est donc pas exactement le même que JSON ref. 

## Le passage de paramètres lors de l'inclusion

Un #ref peut passer des paramètres au fichier inclu via une syntaxe de type url. Par exemple :

    "#ref"      "mon-fichier.vdf?param1=valeur&param2=valeur

Dans le fichier inclu, il est ensuite possible de faire référence à ces paramètres grâce à la syntaxe Handlebars :

    "propriete"     "valeur {{param1}} {{param2}} suite..."
    "#ref"          "fichier.vdf?param={{param2}}"

## L'accès aux variables spécifiques au controlleur

Chaque controlleur à construire est déclaré dans le fichier controllers.json. On y retrouve des variables à placer dans le contexte global Handlebars. Et vous pouvez y faire référence avec la même syntaxe Handlebar :

    {{#if (equals dpadZone "left_trackpad)}}
        "propriete"     "valeur"
    {{/if}}

## Les helpers handlebar

Plusieurs helpers Handlebar ont été ajoutés 

- "defined" pour savoir si une valeur est définie dans le context : {{#if (defined variable)}}...
- "equals" pour savoir si une valeur est égale à une autre : {{#if (equals "1" "2")}}... Ici, "1" et "2" sont des valeurs statiques, mais vous pouvez faire référence à des variables du contexte aussi
- "true" pour savoir si une variable booléenne est définie ET égale à true. {{#if (true backButtons)}}
- "or" pour un OU logique entre plusieurs valeurs : {{#if (or steamcontroller hori xboxelite)}}...
- "key" pour récupérer une touche depuis le contexte clavier (dépendant de la langue) : {{key "staging"}}
- "layerPos" pour récupérer la position (1-based) d'un layer dans la liste des presets, à partir de son nom (voir la section "Les layers") : {{layerPos "FlightRightClickControls"}}

## La génération de plusieurs controlleurs

Un fichier controllers.json permet de définir l'ensemble des controlleurs à générer, en donnant son nom, et un chemin vers un fichier VDF racine.

Mais à chaque controlleur, il est en plus possible d'ajouter un "contexte" = un ensemble de clés valeurs qui pourront être utilisées à l'intérieur des fichiers VDF. Pour cela, on utilise la bibliothèque externe "handlebars". Et ces couples de clés/valeurs ne sont rien d'autre que le contexte handlebar.
