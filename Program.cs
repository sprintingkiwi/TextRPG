using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Utilities;
using Items;
using Characters;
using Spells;
using Scenes;



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
        public Character.EquipSlot requiredSlot;
        public int speed = 0;

        public virtual void Equip(Character character)
        {            
            character.equipment.Add(requiredSlot, this);
            character.speed.SetModifier(speed, this);
        }

        public virtual void Unequip(Character character)
        {
            character.equipment.Remove(requiredSlot);
            foreach (Stat stat in character.stats)
                stat.RemoveModifierFromSource(this);
        }
    }

    #region Weapons
    public abstract class Weapon : Equipment
    {
        public enum DamageType { Slashing, Bludgeoning, Piercing, None }

        public DamageType damageType; // Type of damage (Slashing, Piercing, ecc.)
        public DamageType weakness;
        public int attack;
        public int rndRange; // Amount of random in damage
        public int precision; // Chances of hitting the target (%)
        //public int ap = 1; // Action Points required to use this weapon

        public Weapon()
        {
            requiredSlot = Character.EquipSlot.Weapon;
        }

        public Weapon(string name, Character.EquipSlot requiredSlot, int attack, DamageType damageType, DamageType weakness, int speed, int rndRange, int precision, int value)
        {
            this.name = name;
            this.requiredSlot = requiredSlot;
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

    public class NaturalWeapon : Weapon
    {
        public NaturalWeapon(int precision, int rndRange) : base()
        {
            this.precision = precision;
            this.rndRange = rndRange;
            damageType = DamageType.None;
            weakness = DamageType.None;
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
            value = 50;
            attack = 40;
            speed = -30;
        }
    }


    public abstract class Spear : Weapon
    {
        public Spear() : base()
        {
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

        public Armor(string name, Character.EquipSlot requiredSlot, int defense, int speed, int value)
        {
            this.name = name;
            this.requiredSlot = requiredSlot;
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
            requiredSlot = Character.EquipSlot.Body;
        }
    }

    public class LeatherArmor : BodyArmor
    {
        public LeatherArmor()
        {
            name = "Leather Armor";
            value = 5;
            defense = 3;
            speed = -2;
        }
    }

    public abstract class Shield : Armor
    {
        public Shield() : base()
        {
            requiredSlot = Character.EquipSlot.Shield;
        }
    }

    public class WoodenShield : Shield
    {
        public WoodenShield() : base()
        {
            name = "Wooden Shield";
            value = 5;
            defense = 6;
            speed = -5;
        }
    }
    #endregion

    #region Accessories
    public class Accessory : Equipment
    {
        public Accessory()
        {
            requiredSlot = Character.EquipSlot.Accessory;
        }
    }

    public class PowerNecklace : Accessory
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
        private int baseValue;
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
        public static int BaseUnarmedCombatDamage = 10;

        public string name;
        //public int level = 1;
        protected int damage; // Amount of damage taken so far
        public Spell.Element elementWeakness = Spell.Element.None;
        public Spell.Element elementResistance = Spell.Element.None;
        public readonly Stat maxHP = new Stat(); // Total hit points
        public readonly Stat attack = new Stat();
        public readonly Stat defense = new Stat();
        public readonly Stat speed = new Stat();
        public readonly Stat[] stats;
        public enum EquipSlot { Head, Body, Shield, Weapon, Accessory }
        public Dictionary<EquipSlot, Equipment> equipment = new Dictionary<EquipSlot, Equipment>(); // Dictionary from slot name to item class instance
        protected List<Spell> spellbook = new List<Spell>(); // Known spells

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
            return name + " - HP: " + CurrentHP.ToString();
        }

        public abstract string LogAction(string actionName);

        public int CurrentHP
        { 
            get
            {
                int hp = maxHP.Value - damage;
                if (hp < 0) { hp = 0; } // Prevent negative Hit Points
                return hp;
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (damage < 1) { damage = 1; } // Prevent negative or null damage values
            this.damage += damage;
            Utils.Tale(LogAction("receive") + " " + damage.ToString() + " points of damage");
        }

        public void Attack(Character target)
        {
            Weapon weapon = equipment[EquipSlot.Weapon] as Weapon;
            Weapon targetWeapon = target.equipment[EquipSlot.Weapon] as Weapon;

            string attackLog = LogAction("attack") + " " + target.name;
            if(!(weapon is NaturalWeapon)) { attackLog += " with " + weapon.name; }
            Utils.Tale(attackLog);

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
                target.TakeDamage(damage);
            }
            else { Utils.Tale("The attack misses..."); }
        }

        public virtual void LearnSpell(Spell spell)
        {
            Utils.Tale("Learned spell: " + spell.name);
            spellbook.Add(spell);
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
            target.TakeDamage(damage);
        }

        public void DrinkPotion()
        {
            Utils.Tale(LogAction("drink") + " a Potion");
            damage = 0;
            Utils.Tale(name + " HP: " + CurrentHP);
        }

        enum BattleOutcome { Continue, Win, Lose }

        public Character Battle(Enemy enemy)
        {
            Utils.Tale("Starting battle with " + enemy.name);

            BattleOutcome outcome = BattleOutcome.Continue;
            BattleOutcome CheckOutcome()
            {
                if (CurrentHP <= 0) { return BattleOutcome.Lose; }
                else if (enemy.CurrentHP <= 0) { return BattleOutcome.Win; }
                else { return BattleOutcome.Continue; }
            }

            // Fight
            bool playerTurn = speed.Value >= enemy.speed.Value;
            while (outcome == BattleOutcome.Continue)
            {
                if (playerTurn)
                {
                    ChooseBattleAction(enemy);
                    Utils.Tale(enemy.ShowStatus());
                    outcome = CheckOutcome();
                }
                else
                {
                    enemy.Attack(this);
                    Utils.Tale(ShowStatus());
                    outcome = CheckOutcome();
                }
                playerTurn = !playerTurn;
            }

            // Outcome
            Console.WriteLine("Battle ended");
            if (outcome == BattleOutcome.Win)
            {
                return this;
            }
            else
            {
                return enemy;
            }
        }
    }

    [System.Serializable]
    public class Player : Character
    {
        protected List<Item> Inventory { get; set; }

        public Player() : base()
        {
            name = "You";
            maxHP.SetBaseValue(50);
            attack.SetBaseValue(10);
            defense.SetBaseValue(5);
            speed.SetBaseValue(5);
            Inventory = new List<Item>();
        }

        public override string LogAction(string actionName)
        {
            return "You " + actionName;
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);

            // END GAME IF YOU DIE
            if (CurrentHP <= 0)
            {
                Utils.Tale("You died.");
                GameController.gameOver = true;
            }                
        }

        public override string[] GetBattleActions()
        {
            string[] availableActions = new string[]
            {
                "Attack",
                "Cast Spell",
                "Drink Potion",
                "Change Weapon"
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
                case "Change Weapon":
                    ChangeEquipment(ChooseItemFromList<Equipment>(GetEquipmentFromInventory(EquipSlot.Weapon)));
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

        public void AddToInventory(Item item)
        {
            if (Inventory.Count <= 10)
            {
                Inventory.Add(item);
                Utils.Tale(item.name + " added to the inventory.");
            }
            else
            {
                Utils.Tale("You cannot take " + item.name + ": inventory is full.");
            }
        }

        public void RemoveFromInventory(Item item)
        {
            Inventory.Remove(item);
        }

        public List<Item> GetItemsFromInventory<itemType>() where itemType : Item
        {
            // Get list of EquipType type of items in inventory
            List<itemType> availableEquips = new List<itemType>();
            foreach (Item item in Inventory)
                if (item is itemType) { availableEquips.Add(item as itemType); }

            return availableEquips as List<Item>;
        }

        public List<Equipment> GetEquipmentFromInventory(EquipSlot slotType)
        {
            // Get list of EquipType type of items in inventory
            List<Equipment> availableEquips = new List<Equipment>();
            foreach (Item item in Inventory)
                if (item is Equipment)
                {
                    Equipment equip = item as Equipment;
                    if (equip.requiredSlot == slotType)
                        availableEquips.Add(equip);
                }

            return availableEquips;
        }

        public ItemType ChooseItemFromList<ItemType>(List<ItemType> availableItems) where ItemType : Item
        {
            int choice = Utils.ProcessChoice(availableItems.ConvertAll(x => x.name).ToArray());
            return availableItems[choice];
        }

        public void ChangeEquipment(Equipment newEquip)
        {
            RemoveFromInventory(newEquip);
            if (equipment.ContainsKey(newEquip.requiredSlot))
            {
                Equipment oldEquip = equipment[newEquip.requiredSlot];
                AddToInventory(oldEquip);
                oldEquip.Unequip(this);
            }
            newEquip.Equip(this);

            Utils.Tale("Equipped " + newEquip.name);
        }        
    }

    #region Enemies
    public abstract class Enemy : Character
    {
        public Enemy(Dictionary<EquipSlot, Equipment> equip = null, List<Spell> spellbook = null) : base()
        {
            if (equip != null)
                foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
                    e.Value.Equip(this);
            else
                SetNaturalWeapon(90, 15);
            
            if (spellbook != null)
                this.spellbook = spellbook;
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

        public void SetNaturalWeapon(int precision, int rndRange)
        {
            equipment[EquipSlot.Weapon] = new NaturalWeapon(precision, rndRange);
        }
    }

    public class Goblin : Enemy
    {
        public Goblin(Dictionary<EquipSlot, Equipment> equip, List<Spell> spellbook = null) : base(equip, spellbook)
        {
            name = "Goblin";
            elementWeakness = Spell.Element.Water;
            maxHP.SetBaseValue(30);
            attack.SetBaseValue(5);
            defense.SetBaseValue(5);
            speed.SetBaseValue(15);
        }
    }

    public class GoblinShaman : Goblin
    {
        public GoblinShaman(List<Spell> spellbook, Dictionary<EquipSlot, Equipment> equip = null) : base(equip, spellbook)
        {
            name = "Goblin Shaman";
            elementWeakness = Spell.Element.None;
            speed.SetBaseValue(15);
            maxHP.SetBaseValue(60);
        }
    }

    public class Wolf : Enemy
    {
        public Wolf()
        {
            name = "Wolf";
            elementWeakness = Spell.Element.Fire;
            maxHP.SetBaseValue(50);
            attack.SetBaseValue(30);
            defense.SetBaseValue(20);
            speed.SetBaseValue(15);
            SetNaturalWeapon(85, 20);
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



[System.Serializable]
public class SaveGame
{
    public string SceneToLoad { get; set; }
    public List<string> Achievements { get; set; }
    public int PlayerDamage { get; set; }
    public List<string> PlayerInventory { get; set; }
    public List<string> PlayerEquipment { get; set; }
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
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(3);
            }
            if (stop) { WaitInput(); }
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
    public static string SavePath = "SaveGame.json";
    public static bool gameOver = false;

    public List<Scene> scenes = new List<Scene>();  // List of all scenes
    public Player player;
    public string sceneToLoad;
    public List<string> achievements = new List<string>();

    public void Save(string fileName)
    {
        string jsonString = JsonSerializer.Serialize(new SaveGame()
        {
            SceneToLoad = sceneToLoad,
            Achievements = achievements,
            PlayerDamage = player.maxHP.Value - player.CurrentHP,
            PlayerInventory = player.GetItemsFromInventory<Item>().ConvertAll<string>(x => x.GetType().ToString()),
            PlayerEquipment = player.equipment.Values.ToList().ConvertAll<string>(x => x.GetType().ToString())
        });
        File.WriteAllText(fileName, jsonString);
    }

    public void Load(string fileName)
    {
        string jsonString = File.ReadAllText(fileName);
        SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(jsonString);
        sceneToLoad = saveGame.SceneToLoad;
        achievements = saveGame.Achievements;
    }

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
                Save(SavePath);
                return scene.Run();
            }
        }

        Console.WriteLine(new Exception("Scene not found"));
        gameOver = true;
        return "";
    }    

    public void RunGame()
    {
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
        player.LearnSpell(fireBall);
        //player.LearnSpell(waterSurge);
        player.AddToInventory(new Club());
        player.AddToInventory(new ShortSpear());

        // Test Enemy
        //Enemy testEnemy = new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
        //{
        //    { Character.EquipSlot.Weapon, new Club() }
        //});
        Enemy testEnemy = new Wolf();



        ////////////////////////////////////////////////
        // DEBUG STUFF
        ////////////////////////////////////////////////
        //Console.WriteLine(testEnemy.speed.Value);
        //Console.WriteLine(player.speed.Value);



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
                case 2: player.Battle(testEnemy); break;
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

            player.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new RustySword() }
            }));

            achievements.Add("Hero");
            Utils.Tale("YOU WIN.");
            gameOver = true;
            return "";
        }));



        //////////////////////////////////////////////
        // MAIN LOOP
        //////////////////////////////////////////////
        if (File.Exists(SavePath))
        {
            Utils.Tale("Loading saved game");
            Load(SavePath);
        }
        else
        {
            Utils.Tale("Starting new game");
            sceneToLoad = "Four Graves Inn";
        }

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
        new GameController().RunGame();
    }
}
