using System.Collections.Generic;
using UnityEngine;

public static class NomAleatoire
{
    static List<string> noms = new List<string>()
    {
        "Léa", "Lucas", "Emma", "Noah", "Mia", "Hugo", "Jade", "Louis", "Lina", "Gabriel",
        "Zoé", "Adam", "Chloé", "Raphaël", "Lola", "Arthur", "Nina", "Tom", "Camille", "Ethan",
        "Alice", "Nathan", "Manon", "Théo", "Inès", "Maxime", "Sarah", "Enzo", "Juliette", "Paul",
        "Eva", "Maël", "Eléna", "Axel", "Anna", "Liam", "Clara", "Timéo", "Ambre", "Sacha",
        "Lou", "Mathis", "Romane", "Aaron", "Elsa", "Noé", "Lucie", "Ilan", "Charlotte", "Milan",
        "Célia", "Rayan", "Océane", "Téo", "Ava", "Bastien", "Louna", "Gaspard", "Nora", "Oscar",
        "Zélie", "Malo", "Margot", "Yanis", "Flora", "Eliott", "Maya", "Diego", "Naëlle", "Kylian",
        "Capucine", "Ismaël", "Thaïs", "Victor", "Salomé", "Jules", "Léonie", "Clément", "Agathe", "Tanguy",
        "Maéva", "Simon", "Léon", "Cloé", "Benoît", "Estelle", "Amir", "Lylou", "Robin", "Anouk",
        "Loïc", "Maëlle", "Kenza", "Nino", "Lison", "Baptiste", "Adèle", "Antoine", "Jeanne", "Swan",
        "Marius", "Apolline", "Loris", "Clarisse", "Ayden", "Violette", "Côme", "Alix", "Ilian", "Éléa",
        "Valentin", "Léna", "Corentin", "Énora", "Nicolas", "Yuna", "Adrien", "Myla", "Ewen", "Solène",
        "Isis", "Gaël", "Maïa", "Matthéo", "Amandine", "Samuel", "Lyana", "Émile", "Syrielle", "Thibault",
        "Yohan", "Sirine", "Rémi", "Émilie", "Julian", "Cassandra", "Ibrahim", "Nahia", "Sami", "Naïs",
        "Élie", "Éva", "Ruben", "Melyne", "Ali", "Émilien", "Emy", "Loane", "Yanis", "Morgane",
        "Abel", "Tess", "Johan", "Soraya", "Kais", "Énola", "Jibril", "Shana", "Arsène", "Melyssa",
        "Gaétan", "Elina", "Olivier", "Alyssa", "Sylvain", "Amalya", "Éric", "Romy", "Michel", "Isaline",
        "Sébastien", "Elya", "Pascal", "Daphné", "Jean", "Soline", "André", "Lara", "Gérard", "Lilou",
        "René", "Séréna", "Albert", "Marina", "Christian", "Océane", "Daniel", "Élodie", "Joseph", "Tina",
        "Marcel", "Lætitia", "Luc", "Céline", "Alain", "Laurine", "Pierre", "Noémie", "Bernard", "Héloïse"
    };


    static HashSet<string> nomsUtilisés = new HashSet<string>();

    public static string ObtenirNomUnique()
    {
        if (nomsUtilisés.Count >= noms.Count)
        {
            return $"Inconnu{Random.Range(1000, 9999)}";
        }

        string nom;
        do
        {
            nom = noms[Random.Range(0, noms.Count)];
        } while (nomsUtilisés.Contains(nom));

        nomsUtilisés.Add(nom);
        return nom;
    }

}
