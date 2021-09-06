using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utilities;
using Items;
using Characters;
using Spells;
using Scenes;
using System.Runtime.CompilerServices;
using System.ComponentModel;



////////////////////////////////////////////////
//ITEMS
////////////////////////////////////////////////
namespace Items
{
    public abstract class Item
    {
        public string name;
        public int value;
    }

    public abstract class Equipment : Item
    {
        public Character.EquipSlot[] requiredSlots;
        public int speed = 0;

        public virtual void Equip(Character character)
        {
            foreach (Character.EquipSlot requiredSlot in requiredSlots)
            {
                if (character.equipment.ContainsKey(requiredSlot))
                    character.equipment[requiredSlot] = this;
                else
                    character.equipment.Add(requiredSlot, this);
            }

            character.speed.SetModifier(speed, this);
        }

        public virtual void Unequip(Character character)
        {
            foreach (Character.EquipSlot requiredSlot in requiredSlots)
            {
                character.equipment[requiredSlot] = null;
            }

            foreach (Stat stat in character.stats)
            {
                stat.RemoveModifierFromSource(this);
            }
        }
    }

    #region Weapons
    public abstract class Weapon : Equipment
    {       
        public enum DamageType { Slashing, Bludgeoning, Piercing }

        public DamageType damageType; // Type of damage (Slashing, Piercing, ecc.)
        public DamageType weakness;
        public int attack;
        public int rndRange; // Amount of random in damage
        public int precision; // Chances of hitting the target (%)
        //public int ap = 1; // Action Points required to use this weapon

        public Weapon()
        {
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Weapon };
        }

        public Weapon(string name, Character.EquipSlot[] requiredSlots, int attack, DamageType damageType, DamageType weakness, int speed, int rndRange, int precision, int value)
        {
            this.name = name;
            this.requiredSlots = requiredSlots;
            this.damageType = damageType;
            this.weakness = weakness;
            this.attack = attack;
            this.speed = speed;
            this.rndRange = rndRange;
            this.precision = precision;
            this.value = value;
        }

        public override void Equip(Character character)
        {
            base.Equip(character);
            character.attack.SetModifier(attack, this);
        }
    }

    public abstract class Sword : Weapon
    {
        public Sword() : base()
        {
            damageType = DamageType.Slashing;
            weakness = DamageType.Piercing;
            rndRange = 10;
            precision = 90;
        }
    }

    class RustySword : Sword
    {
        public RustySword() : base()
        {
            name = "Rusty Sword";
            value = 3;
            attack = 15;
        }
    }

    class Dagger : Sword
    {
        public Dagger() : base()
        {
            name = "Dagger";
            rndRange = 5;
            value = 5;
            attack = 10;
            speed = 10;
            precision = 100;
        }
    }

    class Claymore : Sword
    {
        public Claymore() : base()
        {
            name = "Claymore";
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Weapon, Character.EquipSlot.Shield };
            value = 50;
            attack = 40;
            speed = -30;
        }
    }


    public abstract class Spear : Weapon
    {
        public Spear() : base()
        {
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Weapon, Character.EquipSlot.Shield };
            damageType = DamageType.Piercing;
            weakness = DamageType.Bludgeoning;
            rndRange = 20;
            precision = 90;
            speed = -5;
        }
    }

    class ShortSpear : Spear
    {
        public ShortSpear() : base()
        {
            name = "Short Spear";
            value = 7;
            attack = 15;
        }
    }


    public abstract class Hammer : Weapon
    {
        public Hammer() : base()
        {
            damageType = DamageType.Bludgeoning;
            weakness = DamageType.Slashing;
            rndRange = 30;
            precision = 80;
            speed = -10;
        }
    }

    class Club : Hammer
    {
        public Club() : base()
        {
            name = "Club";
            value = 1;
            attack = 20;
        }
    }

    class MorningStar : Hammer
    {
        public MorningStar() : base()
        {
            name = "Morning Star";
            value = 40;
            attack = 35;
            speed = 0;
        }
    }
    #endregion

    #region Armors
    public abstract class Armor : Equipment
    {
        public Armor() { }

        public Armor(string name, Character.EquipSlot[] requiredSlots, int defense, int speed, int value)
        {
            this.name = name;
            this.requiredSlots = requiredSlots;
            this.defense = defense;
            this.speed = speed;
            this.value = value;
        }

        public int defense = 5;

        public override void Equip(Character character)
        {
            base.Equip(character);

            character.defense.SetModifier(defense, this);
        }
    }

    public abstract class BodyArmor : Armor
    {
        public BodyArmor() : base()
        {
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Body };
        }
    }

    public class LeatherArmor : BodyArmor
    {

    }

    public abstract class Shield : Armor
    {
        public Shield() : base()
        {
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Shield };
        }
    }

    public class WoodenShield : Shield
    {
        public WoodenShield() : base()
        {
            defense = 5;
            speed = -5;
        }
    }
    #endregion

    #region Accessories
    public class Necklace : Equipment
    {
        public Necklace()
        {
            requiredSlots = new Character.EquipSlot[] { Character.EquipSlot.Necklace };
        }
    }

    public class PowerNecklace : Necklace
    {
        public override void Equip(Character character)
        {
            base.Equip(character);
            character.maxHP.SetModifier(30, this);
            character.attack.SetModifier(15, this);
        }
    }
    #endregion
}


////////////////////////////////////////////////
//SPELLS
////////////////////////////////////////////////
namespace Spells
{
    public class Spell
    {
        public enum Element { Earth, Water, Fire, Air, None }

        public string name;
        public int damage;
        public Element element;

        public Spell(string name, int damage, Element element)
        {
            this.name = name;
            this.damage = damage;
            this.element = element;
        }
    }
}


////////////////////////////////////////////////
// CHARACTERS
////////////////////////////////////////////////
namespace Characters
{
    public class Stat
    {
        protected int baseValue;

        private Dictionary<Equipment, int> modifiers = new Dictionary<Equipment, int>();

        public int Value
        {
            get
            {
                int finalValue = baseValue;
                foreach (KeyValuePair<Equipment, int> mod in modifiers)
                    finalValue += mod.Value;
                return finalValue;
            }
        }

        public void SetBaseValue(int baseValue)
        {
            this.baseValue = baseValue;
        }

        public void SetModifier(int mod, Equipment source)
        {
            if (modifiers.ContainsKey(source))
                modifiers[source] = mod;
            else
                modifiers.Add(source, mod);
        }

        public void RemoveModifierFromSource(Equipment source)
        {            
            modifiers.Remove(source);
        }
    }

    public abstract class Character
    {       
        public string name;
        //public int level = 1;
        public int hp; // Current hit points
        public Spell.Element elementWeakness = Spell.Element.None;
        public Spell.Element elementResistance = Spell.Element.None;
        public readonly Stat maxHP = new Stat(); // Total hit points
        public readonly Stat attack = new Stat();
        public readonly Stat defense = new Stat();
        public readonly Stat speed = new Stat();
        public readonly Stat[] stats;
        //public int turnAP = 3; // Actions Points gained at the beginning of each turn
        //public int maxAP = 10; // Maximum stackable Action Points
        public enum EquipSlot { Head, Body, Gauntlets, Boots, Shield, Weapon, Necklace, Ring }
        public Dictionary<EquipSlot, Equipment> equipment = new Dictionary<EquipSlot, Equipment>(); // Dictionary from slot name to item class instance
        public List<Spell> spellbook = new List<Spell>(); // Known spells

        public Character() // Characters initialization
        {
            stats = new Stat[] { attack, defense, speed };
        }

        public abstract string[] GetBattleActions();

        public abstract void ChooseBattleAction(Character opponent);

        public virtual void ExecuteBattleAction(int actionID, Character opponent)
        {
            switch (GetBattleActions()[actionID])
            {
                case "Attack":
                    Attack(opponent);
                    break;
                case "Cast Spell":
                    ChooseSpell(opponent);
                    break;
                case "Drink Potion":
                    DrinkPotion();
                    break;
                default:
                    break;
            }
        }

        public string ShowStatus()
        {
            return name + " - HP: " + hp;
        }

        public abstract string LogAction(string actionName);

        public virtual void GetDamage(int damage)
        {
            if (damage < 1) { damage = 1; } // Prevent negative or null damage values
            hp -= damage;
            if (hp < 0) { hp = 0; } // Prevent negative Hit Points
            Utils.Tale(LogAction("receive") + " " + damage.ToString() + " points of damage");
        }

        public void Attack(Character target)
        {
            Utils.Tale(LogAction("attack") + " " + target.name + " with " + equipment[EquipSlot.Weapon].name);
            Weapon weapon = equipment[EquipSlot.Weapon] as Weapon;
            Weapon targetWeapon = target.equipment[EquipSlot.Weapon] as Weapon;
            Random rnd = new Random();
            int hitRoll = rnd.Next(1, 100);
            bool hit = hitRoll <= weapon.precision - target.speed.Value;
            bool critical = hitRoll > 90;
            if (hit)
            {
                int attackValue = rnd.Next(attack.Value - weapon.rndRange, attack.Value + weapon.rndRange + 1);
                int damage = attackValue - target.defense.Value;
                if (targetWeapon.weakness == weapon.damageType)
                {
                    damage = (int)(damage * 1.5f); // Apply weapon weakness
                    Utils.Tale("Weapon Advantage!");
                } 
                if (critical) { damage *= 3; Utils.Tale("CRITICAL HIT!"); } // Apply critical bonus
                target.GetDamage(damage);
            }
            else
            {
                Utils.Tale("The attack misses...");
            }
        }

        public abstract void ChooseSpell(Character target);

        public virtual void CastSpell(Spell spell, Character target)
        {
            // Cast spell
            Utils.Tale(LogAction("cast") + " the spell: " + spell.name);
            int damage = spell.damage;
            if (target.elementWeakness == spell.element) // Apply element weakness
            {
                damage *= 2;
                Utils.Tale("Element Weakness!");
            }
            else if (target.elementResistance == spell.element)
            {
                damage /= 2;
                Utils.Tale("Element Resistance!");
            }
            target.GetDamage(damage);
        }

        public void DrinkPotion()
        {
            Utils.Tale(LogAction("drink") + " a Potion");
            hp = maxHP.Value;
            Utils.Tale(name + " HP: " + hp);
        }
    }


    public class Player : Character
    {
        public Player() : base()
        {
            name = "You";
            hp = 50;
            maxHP.SetBaseValue(50);
            attack.SetBaseValue(10);
            defense.SetBaseValue(5);
            speed.SetBaseValue(5);
        }

        public override string LogAction(string actionName)
        {
            return "You " + actionName;
        }

        public override string[] GetBattleActions()
        {
            string[] availableActions = new string[]
            {
                "Attack",
                "Cast Spell",
                "Drink Potion",
                "Change Equipment"
            };

            return availableActions.ToArray();
        }
                
        public override void ChooseBattleAction(Character opponent) // Get player choice on fight
        {
            // Ask the player to choose among available actions
            ExecuteBattleAction(Utils.ProcessChoice(GetBattleActions()), opponent);
        }

        public override void ExecuteBattleAction(int actionID, Character opponent)
        {
            base.ExecuteBattleAction(actionID, opponent);

            switch (GetBattleActions()[actionID])
            {
                case "Change Equipment":
                    ChangeEquipment();
                    break;
                default:
                    break;
            }
        }

        public override void ChooseSpell(Character target)
        {
            Utils.Tale("Bind the spell to its name:\n", stop: false);
            string spellName = Console.ReadLine();

            // Check if spell is in spellbook
            Spell spell = null;
            foreach (Spell s in spellbook)
                if (s.name == spellName) { spell = s; break; }
            if (spell == null)
            {
                Utils.Tale("You fail to cast the spell...");
                return;
            }

            CastSpell(spell, target);
        }

        public void ChangeEquipment()
        {
            Utils.Tale("Changing equipment - to be implemented");
        }
    }

    #region Enemies
    public abstract class Enemy : Character
    {
        public Enemy(Dictionary<EquipSlot, Equipment> equip) : base()
        {
            foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
            {
                e.Value.Equip(this);
            }
        }

        public override string LogAction(string actionName)
        {
            return name + " " + actionName + "s";
        }

        public override string[] GetBattleActions()
        {
            return new string[] { "Attack" };
        }

        public override void ChooseBattleAction(Character enemy)
        {
            // Now a simple random strategy
            Random rnd = new Random();
            string[] actions = GetBattleActions();
            ExecuteBattleAction(rnd.Next(0, actions.Length), enemy);
        }

        public override void ChooseSpell(Character target)
        {
            throw new NotImplementedException();
        }
    }

    public class Goblin : Enemy
    {
        public Goblin(Dictionary<EquipSlot, Equipment> equip) : base(equip)
        {
            name = "Goblin";
            hp = 30;
            elementWeakness = Spell.Element.Fire;
            maxHP.SetBaseValue(20);            
            attack.SetBaseValue(5);
            defense.SetBaseValue(5);
            speed.SetBaseValue(15);
        }
    }

    public class GoblinShaman : Goblin
    {
        public GoblinShaman(Dictionary<EquipSlot, Equipment> equip) : base(equip)
        {
            hp = 60;
            elementWeakness = Spell.Element.None;
            speed.SetBaseValue(15);
        }
    }
    #endregion
}



////////////////////////////////////////////////
// SCENES
////////////////////////////////////////////////
namespace Scenes
{
    public class Scene
    {
        public string title;
        private Func<string> func;

        public Scene(string title, Func<string> func)
        {
            this.title = title;
            this.func = func;
        }

        public string Run()
        {
            return func();
        }
    }
}



////////////////////////////////////////////////
// UTILITIES
////////////////////////////////////////////////
namespace Utilities
{
    ////////////////////////////////////////////////
    // DICE ROLLS
    ////////////////////////////////////////////////
    public class Die
    {
        public int faces;
        public int quantity;

        public Die(int faces, int quantity = 1)
        {
            this.faces = faces;
            this.quantity = quantity;
        }
    }



    ////////////////////////////////////////////////
    // Static functions
    ////////////////////////////////////////////////
    public static class Utils
    {
        public static int Roll(int faces, int quantity = 1)
        {
            int result = 0;
            for (int i = 0; i < quantity; i++)
            {
                Random rnd = new Random();
                result += rnd.Next(1, faces + 1);
            }
            return result;
        }
        public static int Roll(Die die)
        {
            int result = 0;
            for (int i = 0; i < die.quantity; i++)
            {
                Random rnd = new Random();
                result += rnd.Next(1, die.faces + 1);
            }
            return result;
        }

        public static string WaitInput()
        {
            return Console.ReadLine();
        }

        public static void Tale(string text, bool stop = true)
        {
            //Console.WriteLine(text + "\n");
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(3);
            }

            if (stop)
            {
                WaitInput();
            }
        }

        public static int ProcessChoice(string[] choices)
        {
            int InvalidInput()
            {
                Tale("Invalid input...", stop: false);
                return ProcessChoice(choices);
            }

            string choiceOutput = "";
            for (int i = 0; i < choices.Length; i++)
            {
                choiceOutput += (i + 1).ToString() + ": " + choices[i] + "\n"; // PS: Choices start from 1
            }

            Tale("Choose your action:\n", stop: false);
            Console.WriteLine(choiceOutput);
            string answer = Console.ReadLine();
            int parsedAnswer;
            if (int.TryParse(answer, out parsedAnswer))
            {
                parsedAnswer -= 1; // Choices start from 1
                if (parsedAnswer < 0 || parsedAnswer > choices.Length)
                {
                    return InvalidInput();
                }
                else
                {
                    return parsedAnswer;
                }
            }
            else
            {
                return InvalidInput();
            }
        }
    }
}



////////////////////////////////////////////////
// GAME CONTROLLER
////////////////////////////////////////////////
class GameController
{
    public static GameController Instance;
    public static int BaseUnarmedCombatDamage = 10;

    //public static RustySword RustySword = new RustySword();

    public List<Scene> scenes = new List<Scene>();  // List of all scenes
    public bool gameOver = false;
    public string sceneToLoad;
    public Player player;

    public void AddScene(Scene scene)
    {
        scenes.Add(scene);
    }

    public string RunScene(string title)
    {
        foreach (Scene scene in scenes)
        {
            if (scene.title == title)
            {
                return scene.Run();
            }
        }

        Console.WriteLine(new Exception("Scene not found"));
        gameOver = true;
        return "";
    }

    enum Outcome { Win, Lose, Continue }

    public void Battle(Enemy enemy)
    {
        Utils.Tale("Starting battle with " + enemy.name);

        Outcome outcome = Outcome.Continue;
        Outcome CheckOutcome(Enemy enemy)
        {
            if (player.hp <= 0)
            {
                return Outcome.Lose;
            }
            else if (enemy.hp <= 0)
            {
                return Outcome.Win;
            }
            else
            {
                return Outcome.Continue;
            }
        }

        // Fight
        bool playerTurn = player.speed.Value >= enemy.speed.Value;
        while (outcome == Outcome.Continue)
        {
            if (playerTurn)
            {
                player.ChooseBattleAction(enemy);
                Utils.Tale(enemy.ShowStatus());
                outcome = CheckOutcome(enemy);
            }
            else
            {
                enemy.Attack(player);
                Utils.Tale(player.ShowStatus());
                outcome = CheckOutcome(enemy);
            }
            playerTurn = !playerTurn;
            //Utils.WaitInput();
        }

        // Outcome
        if (outcome == Outcome.Win)
        {
            Console.WriteLine("Battle ended");
        }
        else
        {
            Console.WriteLine("You lose");
            gameOver = true;
        }
    }

    public void RunGame()
    {
        ////////////////////////////////////////////////
        // CREATE ITEMS
        ////////////////////////////////////////////////
        //RustySword rustySword = new RustySword();
        //ShortSpear shortSpear = new ShortSpear();
        //Club club = new Club();
        //MorningStar morningStar = new MorningStar();
        //WoodenShield woodenShield = new WoodenShield();



        ////////////////////////////////////////////////
        // CREATE SPELLS
        ////////////////////////////////////////////////        
        Spell earthquake = new Spell("Earthquake", 30, Spell.Element.Earth);
        Spell waterSurge = new Spell("Water Surge", 30, Spell.Element.Water);
        Spell fireBall = new Spell("Fire Ball", 30, Spell.Element.Fire);
        Spell gustOfWind = new Spell("Gust of Wind", 30, Spell.Element.Air);



        ////////////////////////////////////////////////
        // CREATE PLAYER
        ////////////////////////////////////////////////  
        player = new Player();
        //Goblin goblin = new Goblin();
        new RustySword().Equip(player);
        //morningStar.Equip(player);        
        new WoodenShield().Equip(player);
        player.spellbook.Add(fireBall);

        // Test Enemy
        Enemy testEnemy = new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
        {
            { Character.EquipSlot.Weapon, new Club() }
        });



        ////////////////////////////////////////////////
        // DEBUG STUFF
        ////////////////////////////////////////////////
        //Console.WriteLine(testEnemy.speed.Value);
        //Console.WriteLine(player.speed.Value);
        //Console.WriteLine("Hello");
        //Console.WriteLine("\n"); // First blank new line
        // w = new RustySword();
        //Console.WriteLine(player.ATTACK + "/" + player.DEFENSE);
        //Enemy goblin = new Goblin(new Dictionary<Character.EquipSlot, Item>()
        //{
        //    { Character.EquipSlot.RightHand, new RustySword() }
        //});
        //Console.WriteLine(goblin.ATTACK + "/" + goblin.DEFENSE);



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
                case 2:
                    Battle(testEnemy);
                    break;
            }

            return "Four Graves Inn";
        }));


        AddScene(new Scene("Quest Board", () =>
        {
            Utils.Tale("You look at the Inn's Quest Board:", false);

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
            Utils.Tale("You are yet too weak for this quest.", false);
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

            Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new RustySword() }
            }));

            Utils.Tale("YOU WIN.");
            gameOver = true;
            return "";
        }));



        //addScene("Main Menu", class () {
        //    tale("Main Menu:", false);
        //    return choice({
        //        "Back": "Back",
        //        "Status": "Status",
        //        "Equipment": "Equipment"
        //    });
        //}, nested = true);

        //addScene("Status", class () {
        //    tale("You are Unk.", false);
        //    return choice({
        //        "Back": "Back"
        //    });
        //}, nested = true);

        //addScene("Equipment", class () {
        //    tale("Your equipment consists of a Rusty Sword and a pair of Old Boots.", false);
        //    return choice({
        //        "Back": "Back"
        //    });
        //}, nested = true);

        //addScene("Cave Terror", class () {
        //    tale("You approach a lonely woman with long grey hair, crying face down on a wooden table. When she hears your steps she looks at you, showing a worn out face marked with the traces of a past beauty.");
        //});



        //////////////////////////////////////////////
        // MAIN LOOP
        //////////////////////////////////////////////
        sceneToLoad = "Four Graves Inn";
        while (!gameOver)
        {
            sceneToLoad = RunScene(sceneToLoad);
            //openedScenes[depth] = sceneToLoad;
        }

        Utils.Tale("GAME OVER");
        Utils.Tale("PRESS A KEY TO CLOSE THE GAME");
        Console.ReadLine();
    }    
}



////////////////////////////////////////////////
// START THE GAME
////////////////////////////////////////////////
class Program
{
    static void Main(string[] args)
    {
        GameController.Instance = new GameController();
        GameController.Instance.RunGame();
    }
}
