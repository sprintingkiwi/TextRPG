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
        MaxHP.SetBaseValue(30);
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
}
#endregion

#region MyItems
public class RustySword : Sword
{
    public override string Name => "Rusty Sword";
    public override int Value => 3;
    public override int Attack => 15;
}

public class Dagger : Sword
{
    public override string Name => "Dagger";
    public override int Value => 5;
    public override int Attack => 10;
    public override int RndRange => 5;
    public override int Precision => 100;
    public override int Speed => 10;
}

public class Claymore : Sword
{
    public override string Name => "Claymore";
    public override int Value => 50;
    public override int Attack => 40;
    public override int Speed => -30;
}

public class ShortSpear : Spear
{
    public override string Name => "Short Spear";
    public override int Value => 7;
    public override int Attack => 15;
}

public class Club : Hammer
{
    public override string Name => "Club";
    public override int Value => 1;
    public override int Attack => 20;
}

public class MorningStar : Hammer
{
    public override string Name => "Morning Star";
    public override int Value => 40;
    public override int Attack => 35;
    public override int Speed => 0;
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


////////////////////////////////////////////////
// CREATE GAME
////////////////////////////////////////////////
class MyGame : Game
{
    readonly Player myPlayer = new Player();
    public override Player Player => myPlayer;
    public override string StartScene => "Four Graves Inn";

    public override void Initialize()
    {
        ////////////////////////////////////////////////
        // SETUP PLAYER
        ////////////////////////////////////////////////  
        //Goblin goblin = new Goblin();
        Weapon w = new RustySword();
        Weapon c = new Club();
        w.Equip(Player);
        new WoodenShield().Equip(Player);
        Player.LearnSpell(new FireBall());
        //player.LearnSpell(waterSurge);
        //player.AddToInventory(new Club());
        //player.AddToInventory(new ShortSpear());
        //Console.WriteLine(Player.Attack.Value);
        //Console.WriteLine(Player.stats.Length);
    }

    public override void CreateScenes()
    {
        ////////////////////////////////////////////////
        // CREATE SCENES
        ////////////////////////////////////////////////
        AddScene(new Scene("Four Graves Inn", () =>
        {
            Utils.Tale("You find yourself inside the Four Graves Inn. " +
                "There is a quest board in front of you.");

            switch (Utils.ProcessChoice(new string[]
            {
                "Talk with the innkeeper",
                "Read the quest board",
                "Battle Test"
            }))
            {
                case 0: return "Innkeeper";
                case 1: return "Quest Board";
                case 2: Player.Battle(new Wolf()); break;
            }

            return "Four Graves Inn";
        }));


        AddScene(new Scene("Quest Board", () =>
        {
            Utils.Tale("You look at the Inn's Quest Board:");

            switch (Utils.ProcessChoice(new string[]
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
            Utils.Tale("INNKEEPER: You are a stranger, aren't you?");
            Utils.Tale("You look at the face of a middle-aged man, with great mustaches and a bold head.");
            Utils.Tale("INNKEEPER: Well, if you're searching for a job, there's that woman crying alone on that table. No one's gonna help her and... I honestly couldn't blame them.");
            Utils.Tale("INNKEEPER: It's also written on the quest board");
            switch (Utils.ProcessChoice(new string[]
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
            Utils.Tale("You approach a lonely woman with long grey hair, crying face down on a wooden table.");
            Utils.Tale("When she hears your steps she looks at you, showing a worn out face marked with the traces of a past beauty.");
            Utils.Tale("WOMAN: Please save my daughter!");
            Utils.Tale("WOMAN: She suddenly disappeared... But I believe she was kidnapped but some goblin and taken to Cave Terror!");

            switch (Utils.ProcessChoice(new string[]
            {
                "Back",
                "Rescue her daughter",
                "Ask for more informations"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Cave Terror";
                case 2: break;
            }

            achievements.Add("Careful listener");
            Utils.Tale("WOMAN: You can find Cave Terror North-East from here, right after a very big Oak");
            Utils.Tale("WOMAN: Those goblins are horrible creatures... I don't want to think what they could have done to my daughter!");
            Utils.Tale("WOMAN: PLEASE SAVE HER!");

            switch (Utils.ProcessChoice(new string[]
            {
                "Back",
                "Rescue her daughter"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: return "Cave Terror";
            }

            return "";
        }));


        AddScene(new Scene("Dragon", () =>
        {
            achievements.Add("Reckless novice");
            Utils.Tale("You are yet too weak for this quest.");
            switch (Utils.ProcessChoice(new string[] { "Back" })) { case 0: return "Quest Board"; }
            return "";
        }));


        AddScene(new Scene("Cave Terror", () =>
        {
            Utils.Tale("You reach Cave Terror after walking for an hour.");
            Utils.Tale("You enter a damp cave. The suddenly cold air and the creepy noise you hear from distance make you feel uneasy.");

            switch (Utils.ProcessChoice(new string[]
            {
                "Go back",
                "Move on"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: break;
            }

            Utils.Tale("As you move forward, deeper into the cave, you hear the sound of fast steps approaching...");
            Utils.Tale("IT'S A GOBLIN!");
            Utils.Tale("And he's not friendly. The Goblin attacks you!");

            Player.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new RustySword() }
            }));

            achievements.Add("Hero");
            Utils.Tale("YOU WIN.");
            gameOver = true;
            return "";
        }));


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
