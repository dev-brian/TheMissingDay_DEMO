// Todos los textos de los tres niveles (documento técnico v2 del equipo), ya escritos.
// En el Inspector de cada Clue y del LevelManager solo eliges el "Preset" y todo se llena al dar Play.
// Si quieres cambiar una frase, cámbiala aquí.
public enum ClueId
{
    Personalizada,
    N1_Escritorio, N1_Poster, N1_Camara, N1_Collar,
    N2_Fotografia, N2_Carta,
    N3_Hoja1, N3_Hoja2, N3_Hoja3, N3_Hoja4, N3_Hoja5
}

public enum LevelId { Personalizado, Nivel1, Nivel2, Nivel3 }

public static class ClueLibrary
{
    static GameUI.Question Q(string id, string instruction, string text, string a, string b, string c,
                             int correct, string good, string bad)
    {
        return new GameUI.Question
        {
            id = id, instruction = instruction, text = text, options = new[] { a, b, c },
            correct = correct, feedbackCorrect = good, feedbackWrong = bad
        };
    }

    const string Complete = "Complete the sentence:";
    const string Choose = "Choose the correct answer.";

    public static void Apply(Clue c)
    {
        c.hasQuestion = true;
        c.countsAsClue = true;
        c.hideWhenDone = false;
        c.before = new string[0];
        c.nextObjective = "";
        switch (c.preset)
        {
            // ---------------- NIVEL 1: MY DREAM ----------------
            case ClueId.N1_Escritorio:
                c.clueName = "My Desk";
                c.countsAsClue = false;
                c.hasQuestion = false;
                c.after = new[] { "I love movies.", "One day, I want to make my own movies.", "I want to tell my own stories." };
                c.nextObjective = "Look at your movie project.";
                break;
            case ClueId.N1_Poster:
                c.lookBack = true;
                c.clueName = "My Own Story";
                c.description = "The poster of Enedina's movie project: MY OWN STORY.";
                c.before = new[] { "This is my movie poster!" };
                c.question = Q("N1_Poster", Complete, "Enedina _____ stories.", "write", "writes", "writing", 1,
                    "Correct! Enedina writes stories.", "Try again! Enedina = she. She + verb + s.");
                c.after = new[] { "This is my movie.", "I write the story.", "I make the characters." };
                c.nextObjective = "Find your camera.";
                break;
            case ClueId.N1_Camara:
                c.clueName = "My Camera";
                c.description = "Enedina's camera. She uses it to make videos.";
                c.before = new[] { "My camera!" };
                c.question = Q("N1_Camara", Complete, "She _____ videos with her camera.", "make", "makes", "making", 1,
                    "Great! She makes videos with her camera.", "Try again! Remember: he, she, it + verb-s.");
                c.after = new[] { "This is my camera.", "I use it to make videos." };
                c.nextObjective = "Explore your room.";
                break;
            case ClueId.N1_Collar:
                c.clueName = "The Star Necklace";
                c.description = "An old golden necklace with a star. It was in a box.";
                c.before = new[] { "What is in this box?", "A necklace... with a star!" };
                c.question = Q("N1_Collar", Complete, "The necklace _____ like a star.", "look", "looks", "looking", 1,
                    "Excellent! The necklace looks like a star.", "Try again! 'The necklace' = it. It + verb-s.");
                c.after = new[] { "It looks very old.", "It is not my necklace.", "But... whose necklace is this?" };
                break;

            // ---------------- NIVEL 2: THE OLD PHOTOGRAPH ----------------
            case ClueId.N2_Fotografia:
                c.lookBack = true;
                c.clueName = "The Old Photograph";
                c.description = "An old family photograph on Grandma's cabinet.";
                c.question = Q("N2_Fotografia", Choose, "What is this?",
                    "It is a photograph.", "It are a photograph.", "It am a photograph.", 0,
                    "Correct! It is a photograph.", "Try again! With 'it', use 'is'.");
                c.after = new[] { "This is an old photograph.", "Who is in this picture?", "I need to know more." };
                break;
            case ClueId.N2_Carta:
                c.clueName = "The Old Letter";
                c.description = "An old letter kept with the family's belongings.";
                c.question = Q("N2_Carta", Choose, "Which sentence is correct?",
                    "The letter are old.", "The letter am old.", "The letter is old.", 2,
                    "Well done! The letter is old.", "Try again! 'The letter' = it. Use 'is'.");
                c.after = new[] { "This is an old letter.", "Who writes this letter?", "Maybe it can help me." };
                break;

            // ---------------- NIVEL 3: THE FIVE SCRIPT PAGES ----------------
            case ClueId.N3_Hoja1:
                Page(c, 1, new[] { "Here is the first page!", "Let's see what it says." },
                    Q("N3_Hoja1", Complete, "Enedina _____ movies.", "love", "loves", "loving", 1,
                      "Correct! Enedina loves movies.", "Try again! Remember the third-person singular rule."));
                break;
            case ClueId.N3_Hoja2:
                Page(c, 2, new[] { "I found another page!", "This script has more clues." },
                    Q("N3_Hoja2", Complete, "She _____ a movie script.", "have", "has", "haves", 1,
                      "Great! She has a movie script.", "Try again! Remember: he, she, it + has."));
                break;
            case ClueId.N3_Hoja3:
                Page(c, 3, new[] { "Here is the third page.", "I wonder who writes this story." },
                    Q("N3_Hoja3", Complete, "They _____ old stories.", "likes", "liking", "like", 2,
                      "Well done! They like old stories.", "Try again! With 'they', use the base form of the verb."));
                break;
            case ClueId.N3_Hoja4:
                Page(c, 4, new[] { "Only two pages left!", "I am getting closer." },
                    Q("N3_Hoja4", Complete, "My grandmother _____ the necklace.", "keep", "keeps", "keeping", 1,
                      "Excellent! My grandmother keeps the necklace.", "Try again! 'My grandmother' is third-person singular."));
                break;
            case ClueId.N3_Hoja5:
                Page(c, 5, new[] { "The last page!", "Let's complete the script." },
                    Q("N3_Hoja5", Complete, "Enedina _____ to find the truth.", "wants", "want", "wanting", 0,
                      "Perfect! Enedina wants to find the truth.", "Try again! Remember to use 'wants' with Enedina."));
                break;
        }
    }

    static void Page(Clue c, int n, string[] before, GameUI.Question q)
    {
        c.clueName = "Script Page " + n;
        c.description = "Page " + n + " of an old movie script.";
        c.before = before;
        c.question = q;
        c.after = new string[0];
        c.hideWhenDone = true;
        c.promptText = "Press E to collect";
    }

    public static void Apply(LevelManager m)
    {
        switch (m.preset)
        {
            case LevelId.Nivel1:
                m.levelNumber = "LEVEL 1";
                m.levelTitle = "MY DREAM";
                m.mission = "Explore your room.";
                m.introLines = new[] { "Good morning!", "Today I want to work on my movie." };
                m.counterLabel = "Clues";
                m.totalClues = 3;
                m.completeLines = new[] { "I want to make my own movies.", "This is my dream.",
                                          "But this necklace... I need to ask Grandma about it." };
                m.achievementName = "Dreamer";
                m.achievementText = "You found your three movie treasures!";
                m.afterObjective = "Go to the door. Visit Grandma's house.";
                break;
            case LevelId.Nivel2:
                m.levelNumber = "LEVEL 2";
                m.levelTitle = "THE OLD PHOTOGRAPH";
                m.mission = "Find the old photograph.";
                m.introLines = new[] { "That necklace is very old.", "Why does Grandma keep it?", "Maybe I can find a clue here." };
                m.counterLabel = "Clues";
                m.totalClues = 2;
                m.completeLines = new[] { "These clues are interesting.", "Maybe there is more to this story.", "I need to keep looking." };
                m.achievementName = "Family Detective";
                m.achievementText = "You found two family clues!";
                m.afterObjective = "Continue your investigation. Go to the study (door on the left).";
                break;
            case LevelId.Nivel3:
                m.levelNumber = "LEVEL 3";
                m.levelTitle = "THE FIVE SCRIPT PAGES";
                m.mission = "Collect the five script pages.";
                m.introLines = new[] { "I have an old script. It is very interesting.", "But some pages are missing.", "I need to find all five pages." };
                m.counterLabel = "Script Pages";
                m.totalClues = 5;
                m.completeLines = new[] { "I found all five pages!", "Now I can read the whole script." };
                m.achievementName = "Script Collector";
                m.achievementText = "You collected all five script pages!";
                m.afterObjective = "";
                break;
        }
    }
}
