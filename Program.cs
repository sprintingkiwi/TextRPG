using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.IO;
using TextRPG;


////////////////////////////////////////////////
// CREATE CUSTOM CLASSES
////////////////////////////////////////////////
#region MyEnemies
public class Goblin : Enemy
{

    public override string Name => "Goblin";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;
    public override Spell.ElementType ElementResistance => Spell.ElementType.Earth;

    public Goblin(Dictionary<EquipSlot, Equipment> equip, List<Spell> spellbook = null) : base(equip, spellbook)
    {
        MaxHP.SetBaseValue(50);
        Attack.SetBaseValue(5);
        Defense.SetBaseValue(5);
        Speed.SetBaseValue(15);
    }
}

public class GoblinShaman : Goblin
{
    public override string Name => "Goblin Shaman";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.None;

    public GoblinShaman(List<Spell> spellbook, Dictionary<EquipSlot, Equipment> equip = null) : base(equip, spellbook)
    {
        MaxHP.SetBaseValue(60);
        Speed.SetBaseValue(15);
    }
}

public class Wolf : Enemy
{
    public override string Name => "Wolf";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;
    public override Spell.ElementType ElementResistance => Spell.ElementType.Air;

    public Wolf()
    {
        MaxHP.SetBaseValue(50);
        Attack.SetBaseValue(30);
        Defense.SetBaseValue(20);
        Speed.SetBaseValue(15);
        NaturalWeapon = new NaturalWeapon(rndRange: 25, precision: 80);
    }
}
#endregion

#region MySpells
public class FireBall : Spell
{
    public override string Name => "Fire Ball";
    public override int Damage => 30;
    public override ElementType Element => ElementType.Fire;
}

public class GustOfWind : Spell
{
    public override string Name => "Gust Of Wind";
    public override int Damage => 30;
    public override ElementType Element => ElementType.Air;
}

public class WaterSurge : Spell
{
    public override string Name => "Water Surge";
    public override int Damage => 30;
    public override ElementType Element => ElementType.Water;
}

public class Earthquake : Spell
{
    public override string Name => "Earthquake";
    public override int Damage => 30;
    public override ElementType Element => ElementType.Earth;
}
public class Meteor : Spell
{
    public override string Name => "MeT3 0r!";
    public override int Damage => 200;
    public override ElementType Element => ElementType.Fire;
    public override Action<Character, Character> CustomEffects => (user, target) =>
    {
        Game.Instance.Tale("Custom Effects for user: " + user.Name);
        Game.Instance.Tale("Custom Effects for target: " + target.Name);
    };
}
#endregion

#region MyItems
public class RustySword : Sword
{
    public override string Name => "Rusty Sword";
    public override int Value => 3;
    public override int Attack => 15;
    public override int Durability => 10;
}

public class Dagger : Sword
{
    public override string Name => "Dagger";
    public override int Value => 5;
    public override int Attack => 10;
    public override int RndRange => 5;
    public override int Precision => 100;
    public override int Speed => 10;
    public override int Durability => 20;
}

public class Claymore : Sword
{
    public override string Name => "Claymore";
    public override int Value => 50;
    public override int Attack => 40;
    public override int Speed => -30;
    public override int Durability => 20;
}

public class ThunderBlade : Sword
{
    public override string Name => "Thunder Blade";
    public override int Value => 350;
    public override int Attack => 30;
    public override int Speed => 10;
    public override int Durability => 10;
    public override Action<Character, Character> CustomEffects => (user, target) =>
    {
        Game.Instance.Tale("A thunder strikes " + target.Name + "!");
        target.TakeDamage(
            Character.MagicDamage(20, Spell.ElementType.Air, target.ElementWeakness, target.ElementResistance),
            "Magic "
            );
    };
}

public class ShortSpear : Spear
{
    public override string Name => "Short Spear";
    public override int Value => 7;
    public override int Attack => 15;
    public override int Durability => 20;
}

public class Club : Hammer
{
    public override string Name => "Club";
    public override int Value => 1;
    public override int Attack => 20;
    public override int Durability => 10;
}

public class MorningStar : Hammer
{
    public override string Name => "Morning Star";
    public override int Value => 40;
    public override int Attack => 35;
    public override int Speed => 0;
    public override int Durability => 20;
}

public abstract class Axe : Weapon
{
    public override DamageType DmgType => DamageType.Slashing;
    public override DamageType Weakness => DamageType.Slashing;
    public override int RndRange => 25;
    public override int Precision => 80;
    public override int Speed => -7;
}

public class HandAxe : Axe
{
    public override string Name => "Hand Axe";
    public override int Value => 5;
    public override int Attack => 25;
    public override int Durability => 20;
}

public abstract class Bow : Weapon
{
    public override DamageType DmgType => DamageType.Piercing;
    public override DamageType Weakness => DamageType.Bludgeoning;
    public override int RndRange => 10;
    public override int Precision => 60;
    public override int Speed => 20;
    public override void Equip(Character character)
    {
        base.Equip(character);
        character.Defense.SetModifier(-10, this);
    }
}

public class ShortBow : Bow
{
    public override string Name => "Short Bow";
    public override int Value => 20;
    public override int Attack => 40;
    public override int Durability => 10;
}

public class LeatherArmor : BodyArmor
{
    public override string Name => "Leather Armor";
    public override int Value => 5;
    public override int Defense => 3;
    public override int Speed => -2;
}

public class WoodenShield : Shield
{
    public override string Name => "Wooden Shield";
    public override int Value => 5;
    public override int Defense => 6;
    public override int Speed => -5;
    public override int Durability => 3;
}

public class IronShield : Shield
{
    public override string Name => "Iron Shield";
    public override int Value => 20;
    public override int Defense => 15;
    public override int Speed => -7;
    public override int Durability => 6;
}

public class PowerNecklace : Accessory
{
    public override string Name => "Power Necklace";
    public override int Value => 100;

    public override void Equip(Character character)
    {
        base.Equip(character);
        character.MaxHP.SetModifier(30, this);
        character.Attack.SetModifier(15, this);
    }
}
#endregion

public class MyPlayer : Player
{
    protected override void CastSpell(Spell spell, Character target)
    {
        base.CastSpell(spell, target);

        // Here my custom spells system
    }

    public override string ShowStatus()
    {
        string status = "Your name is Kros\nHP: " + CurrentHP.ToString() + "\nMana Points: " + manaPoints.ToString() + "\nCurrent Equipment:\n";
        foreach (EquipSlot equip in equipment.Keys)
            status += equip.ToString() + " => " + equipment[equip].ToString() + "\n";

        return status;
    }
}

////////////////////////////////////////////////
// CREATE GAME
////////////////////////////////////////////////
class MyGame : Game
{
    Player myPlayer = new MyPlayer();
    protected override Player Player => myPlayer;
    protected override string StartScene => "Intro";

    // HERE YOUR CUSTOM WAY TO OUTPUT TEXT
    //public override void Tale(string text, bool stop = true)
    //{
    //    Console.WriteLine("CIAO");
    //    base.Tale(text, stop);
    //}

    protected override void MainMenu()
    {
        Tale("Welcome to this Text Role-Playing Game!");
        switch (ProcessChoice(new string[]
        {
            "New game",
            "Continue",
            "Credits",
            "Quit",
            "Battle Test"
        }))
        {
            case 0:
                if (File.Exists(SavePath))
                {
                    Tale("There's already a save file. Do you want to delete it and start a new game?");
                    Tale("If you say yes, you will lose your past game progress!");
                    switch (ProcessChoice(new string[]
                    {
                        "YES",
                        "NO"
                    }))
                    {
                        case 0: File.Delete(SavePath); break;
                        case 1: MainMenu(); break;
                    }
                }
                Tale("Starting new game");
                sceneToLoad = StartScene;
                Initialize();
                break;
            case 1:
                if (File.Exists(SavePath))
                {
                    Tale("Loading saved game");
                    Load(SavePath);
                }                
                else
                {
                    Tale("There's no save file to load");
                    MainMenu();
                }
                break;
            case 2:
                Tale("This game was realized by Sprintingkiwi: https://github.com/sprintingkiwi/TextRPG");
                Tale("You can use this game library TextRPG.cs to program your own text RPG game");
                MainMenu(); break;
            case 3: Environment.Exit(0); break;
            case 4:
                BattleTest();                
                MainMenu(); break;
        }
    }

    void BattleTest()
    {
        // Player settings
        //new ThunderBlade().Equip(Player);
        new RustySword().Equip(Player);
        new WoodenShield().Equip(Player);
        Player.LearnSpell(new FireBall());
        Player.LearnSpell(new Meteor());
        Player.AddToInventory(new Potion(), silent: true);
        Player.AddToInventory(new ThunderBlade(), silent: true);

        // Test enemy
        Enemy testEnemy = new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
        {
            { Character.EquipSlot.Weapon, new ShortSpear() }
        });

        // Battle
        Player.Battle(testEnemy, (outcome, enemy) => {
            if (outcome == Character.BattleOutcome.Win)
            {
                Tale("YOU WIN");
                return Player;
            }                
            else
            {
                Tale("YOU LOSE");
                return enemy;
            }                
        });

        // Restore new player
        myPlayer = new MyPlayer();
    }

    protected override void Initialize()
    {
        ////////////////////////////////////////////////
        // SETUP PLAYER
        ////////////////////////////////////////////////  
        new RustySword().Equip(Player);
        //new WoodenShield().Equip(Player);
        //Player.LearnSpell(new FireBall());
        Player.MaxManaPoints.SetBaseValue(3);
        Player.CurrentManaPoints = 2;
        //player.LearnSpell(waterSurge);
        //player.AddToInventory(new Club());
        //Player.AddToInventory(new Potion());
        Player.GetItemsFromInventory<Potion>();
        //player.AddToInventory(new ShortSpear());
        //Console.WriteLine(Player.Attack.Value);
        //Console.WriteLine(Player.stats.Length);
    }

    protected override void CreateScenes()
    {
        ////////////////////////////////////////////////
        // CREATE SCENES
        ////////////////////////////////////////////////

        #region Prologue
        AddScene(new Scene("Intro", () =>
        {            
            Tale("\nPART 1: KROS\n");
            Tale("PROLOGUE: The hood and the horns\n");

            Tale("You are walking on a muddy path. After a week of travel, you've finally found a place where you can rest.");
            Tale("A wooden building stands in front of you. An inn of some sort.");
            Tale("Your name is");
            Tale("Kros");
            Tale("A gentle female voice whispers something. A voice you are well used to hear.");
            Tale("JUNO: There there, you'll finally eat some food and get some rest. We have the money to rent a room.");
            Tale("There's no one to be seen where the voice comes from.");
            Tale("Just a gentle breeze, moving the hood on your head...");
            
            return "Four Graves Inn";
        }));

        AddScene(new Scene("Four Graves Inn", () =>
        {
            Tale("You find yourself inside the Four Graves Inn. " +
                "There is a quest board in front of you.");

            switch (ProcessChoice(new string[]
            {                
                "View your status",
                "Save Game",
                "Talk with the innkeeper",
                "Read the quest board",                
            }))
            {
                case 0: Tale(Player.ShowStatus()); break;
                case 1: Save(); Tale("You saved the game."); break;
                case 2: return "Innkeeper";
                case 3: return "Quest Board";
            }

            return "Four Graves Inn";
        }));


        AddScene(new Scene("Quest Board", () =>
        {
            Tale("You look at the Inn's Quest Board:");

            switch (ProcessChoice(new string[]
            {
                "Back",
                "Rescue a girl",
                "Defeat the dragon"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Lonely Woman";
                case 2: return "Dragon";
            }

            return "";
        }));


        AddScene(new Scene("Innkeeper", () =>
        {
            Tale("INNKEEPER: You are a stranger, aren't you?");
            Tale("You look at the face of a middle-aged man, with great mustaches and a bold head.");
            Tale("INNKEEPER: Well, if you're searching for a job, there's that woman crying alone on that table. No one's gonna help her and... I honestly couldn't blame them.");
            Tale("INNKEEPER: It's also written on the quest board");
            switch (ProcessChoice(new string[]
            {
                "Back",
                "Approach the woman"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Lonely Woman";
            }

            return "";
        }));


        AddScene(new Scene("Lonely Woman", () =>
        {
            Tale("You approach a lonely woman with long grey hair, crying face down on a wooden table.");
            Tale("When she hears your steps she looks at you, showing a worn out face marked with the traces of a past beauty.");
            Tale("WOMAN: Please save my daughter!");
            Tale("WOMAN: She suddenly disappeared... But I believe she was kidnapped but some goblin and taken to Cave Terror!");

            switch (ProcessChoice(new string[]
            {
                "Back",
                "Rescue her daughter",
                "Ask for more informations"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Cave Terror Intro";
                case 2: break;
            }

            achievements.Add("Careful listener");
            Tale("WOMAN: You can find Cave Terror North-East from here, right after a very big Oak");
            Tale("WOMAN: Those goblins are horrible creatures... I don't want to think what they could have done to my daughter!");
            Tale("WOMAN: PLEASE SAVE HER!");

            switch (ProcessChoice(new string[]
            {
                "Back",
                "Rescue her daughter"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Cave Terror Intro";
            }

            return "";
        }));


        AddScene(new Scene("Dragon", () =>
        {
            achievements.Add("Reckless novice");
            Tale("You are yet too weak for this quest.");
            switch (ProcessChoice(new string[] { "Back" })) { case 0: return "Quest Board"; }
            return "";
        }));

        AddScene(new Scene("Cave Terror Intro", () =>
        {
            Tale("You reach Cave Terror after walking for an hour.");
            Tale("You enter a damp cave. The suddenly cold air and the creepy noise you hear from distance make you feel uneasy.");
            Tale("JUNO: Remember Kros, you have half Kuraktai blood boiling in your veins!");
            Tale("JUNO: Should you find yourself great danger, you can use that spell I taught you. Do you remember its name and how to cast it?");

            switch (ProcessChoice(new string[]
            {
                "Yes",
                "No"
            }))
            {
                case 0:
                    Tale("The Fire Ball is a powerfull spell.");
                    break;
                case 1:
                    Tale("Uhmpf! I knew you weren't paying me attention...");
                    Tale("It's called: Fire Ball.");
                    break;
            }
            Tale("It is a powerfull spell, and it's the signature spell of your Kuraktai ancestry.");
            Tale("Remember, in order to cast a spell, you must bind it to its name, exactly as it is written!");
            Tale("Fire Ball");
            Player.LearnSpell(new FireBall());

            switch (ProcessChoice(new string[]
            {
                "Go back",
                "Move on"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Cave Terror";
            }
            return "";
        }));


        AddScene(new Scene("Cave Terror", () =>
        {
            Tale("As you move forward, deeper into the cave, you hear the sound of fast steps approaching...");
            Tale("IT'S A GOBLIN!");
            Tale("And he's not friendly. The Goblin attacks you!");
            Tale("JUNO: That Goblin is armed with a club!");
            Tale("JUNO: With your sword you should have a weapon advantage on him.");

            Player.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new Club() }
            }));

            Tale("Leaving behind the body of the dead Goblin, you move your steps onward...");
            Tale("The path now splits into two directions.");

            switch (ProcessChoice(new string[]
            {
                "Go Right",
                "Go Left"
            }))
            {
                case 0:
                    Tale("A natural hall filled with glowing blue crystals stands out before your eyes.");
                    Tale("JUNO: Marvelous...");
                    Tale("In a corner you see an armored skeleton: the remainings of an unlucky warrior who came here long time ago.");
                    Tale("JUNO: Kros, look");
                    Tale("Next to the skeleton lie a Spear and a worn out bag.");
                    Tale("JUNO: the Goblins are probably scared by these glowing crystals and never come here...");
                    Tale("JUNO: You should take the Spear and look inside the bag, there could be some useful item");
                    Tale("You approach the skeleton with some disgust and the air begins to smell of mold.");
                    Player.AddToInventory(new ShortSpear());
                    Tale("Inside the bag you find two Potions!");
                    Tale("JUNO: Great! A Potion can be a life saver.");
                    Player.AddToInventory(new Potion());
                    Player.AddToInventory(new Potion());
                    break;
                case 1:
                    Tale("You walk down a narrow corridor. Many raw Clubs are piled up agains the walls.");
                    Tale("Also, a worn out chest lies in corner, filled with spider webs.");
                    Tale("JUNO: You should take one of those Clubs, it could be useful against certain foes.");
                    Player.AddToInventory(new Club());
                    Tale("JUNO: ...And let's check that chest. This seems to be the Goblins armory and that might be their treasure vault.");
                    Tale("You cut the spider webs with your sword and open the chest: there is a Potion inside!");
                    Tale("JUNO: Great! A Potion can be a life saver.");
                    Player.AddToInventory(new Potion());
                    break;
            }

            Tale("GAAAAAAAAAAAAAAAA!");
            Tale("Suddenly you hear a scream. The voice of a young woman...");
            Tale("JUNO: It's the kindapped girl! We must move forward.");
            Tale("You move into a larger room. There's wooden cage: it's opened, and inside you see a scared mud-covered girl, crying as loud as she can.");
            Tale("Before her, another Goblin. As soon as you get closer, the Goblin smells you and turns to you with an angry grin on his face.");
            Tale("JUNO: Watch out Kros! This Goblin is equipped with a Spear...");
            Tale("JUNO: You should change your weapon as soon as the battle starts, or you will suffer a weapon disadvantage!");
            
            Player.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new ShortSpear() }
            }));
            
            Tale("After the defeated Goblin exhales his last breath, you hurry to free the poor girl.");
            Tale("KROS: It's alright girl, your mother sent me here to rescue you.");
            Tale("JUNO: Hush, little jewel, you are safe now...");
            Tale("The girl slowly tries to wipe away the tears with the back of his hands.");
            Tale("GIRL: Who are you? Where did this voice came from?");
            Tale("JUNO: You can't see me now, I am an air elemental.");
            Tale("And saying so, a gentle breeze moves the girl's hair.");
            Tale("GIRL: Air elemental?");
            Tale("KROS: We must hurry out of this place. Can you walk on your feet?");
            Tale("GIRL: Yes...");
            Tale("JUNO: Let's move. We don't know how many Goblins could still be nearby...");           

            Tale("As the three of you approach the much coveted exit of the cave, you suddenly feel an unsettling presence.");
            Tale("You turn back and see two red eyes glowing of that darkenss you were about to leave behind...");
            Tale("JUNO: A Wolf!");
            Tale("JUNO: This Wolf is a fearsome opponent... You might not be able to defeat him with the weapons strenght alone!");
            Tale("JUNO: This could be the very good moment to try out that Fire Ball spell, don't you think?");
            if (Player.CurrentManaPoints < 2)
            {
                Tale("JUNO: I see you already consumed your mana, the magical energy that enables you to cast spells.");
                Tale("JUNO: For this time only I will gift you with a bit of my energy...");
                Player.CurrentManaPoints = 2;
                Tale("JUNO: ...But remember, I won't always be able to do this!");
            }

            Player.Battle(new Wolf());

            achievements.Add("Hero");
            Save();
            Tale("YOU WIN.");
            gameOver = true;
            return "";
        }));
        #endregion

    }
}


////////////////////////////////////////////////
// START THE GAME
////////////////////////////////////////////////
class Program
{
    static void Main(string[] args)
    {
        new MyGame().Run();
    }
}
