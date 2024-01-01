using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TextRPG;

////////////////////////////////////////////////
// CREATE CUSTOM CLASSES
////////////////////////////////////////////////
#region MyCharacters
public class MyCharacter : Character
{
    public MyCharacter()
    {
        MaxHP.SetBaseValue(200);
        Attack.SetBaseValue(10);
        Defense.SetBaseValue(10);
        Speed.SetBaseValue(10);
        MaxManaPoints.SetBaseValue(2);
        Name = "Kros";
    }

    public List<Follower> followers = new List<Follower>();
    public override string Name { get; set; }
    protected int manaPoints = 1;
    public Stat MaxManaPoints { get; } = new Stat("Mana");
    public int CurrentManaPoints
    {
        get { return manaPoints; }
        set
        {
            manaPoints = value;
            if (manaPoints > MaxManaPoints.Value)
                manaPoints = MaxManaPoints.Value;
        }
    }

    protected List<Spell> spellbook = new List<Spell>(); // Known spells
    public List<Spell> Spellbook { get => spellbook; }

    public virtual Spell.ElementType ElementWeakness => Spell.ElementType.None;
    public virtual Spell.ElementType ElementResistance => Spell.ElementType.None;
    public override string LogAction(string actionName)
    {
        return "You " + actionName;
    }

    protected override string[] GetBattleActions()
    {
        List<string> availableActions = new List<string>
        {
                "Attack",
                "Cast Spell",
                "Drink Potion",
                "Change Weapon"
        };

        // Add Assist option when you have at least 1 follower
        if (followers.Count > 0)
            availableActions.Add("Assist");

        return availableActions.ToArray();
    }

    public override void ChooseBattleAction(Character opponent) // Get player choice on fight
    {
        // Ask the player to choose among available actions
        ExecuteBattleAction(Game.Instance.ProcessChoice(GetBattleActions(), "Choose your action:"), opponent);
    }
    public override void ShowStatus()
    {
        string status = "Your name is " + Name + "\nHP: " + CurrentHP.ToString() + "\nMana Points: " + manaPoints.ToString() + "\nCurrent Equipment:";
        foreach (EquipSlot slot in equipment.Keys)
            status += "\n" + slot.ToString() + " => " + equipment[slot].ToString() + equipment[slot].PrintHealth;
        Game.Instance.Tale(status);

        switch (Game.Instance.ProcessChoice(new string[]
        {
            "Change Equipment",
            "Back"
        }))
        {
            case 0: ChangeEquipment(); ShowStatus(); break;
            case 1: break;
        }
    }

    public virtual void LearnSpell(Spell spell, bool silent = false)
    {
        spellbook.Add(spell);
        if (!silent)
            Game.Instance.Tale("Learned spell: " + spell.Name);
    }


    protected override void ExecuteBattleAction(int actionID, Character opponent)
    {
        base.ExecuteBattleAction(actionID, opponent);

        switch (GetBattleActions()[actionID])
        {
            case "Attack":
                new WeaponAttack().Effect(this, opponent);
                break;
            case "Change Weapon":
                if (GetEquipmentFromInventory(EquipSlot.Weapon).Count > 0)
                    EquipFromInventory(ChooseItemFromList(GetEquipmentFromInventory(EquipSlot.Weapon)));
                else
                    Game.Instance.Tale("You don't have any weapon in your inventory.");
                break;
            case "Cast Spell":
                ChooseSpell(opponent);
                break;
            case "Drink Potion":
                DrinkPotion();
                break;
            case "Assist":
                int assistantChoice = Game.Instance.ProcessChoice(followers.ConvertAll(x => x.Name).ToArray());
                Follower assistant = followers[assistantChoice];
                Game.Instance.Tale("Receiving assist from " + assistant.Name + "!");
                assistant.AssistAttack(opponent);
                break;
            default:
                break;
        }
    }

    void ChooseSpell(Character target)
    {
        Game.Instance.Tale("Bind the spell to its name:\n", stop: false);
        string spellName = Game.Instance.WaitInput();

        // Check if spell is in spellbook
        Spell spell = null;
        foreach (Spell s in spellbook)
            if (s.Name == spellName) { spell = s; break; }
        if (spell == null)
        {
            Game.Instance.Tale("You fail to cast the spell...");
            return;
        }

        //Check mana
        if (spell.ManaCost > manaPoints)
        {
            Game.Instance.Tale(spell.Name + " spell casting failed: not enough mana");
            return;
        }
        else
            manaPoints -= spell.ManaCost;

        spell.Effect(this, target);
    }

    void DrinkPotion()
    {
        List<Potion> potions = GetItemsFromInventory<Potion>();
        if (potions.Count >= 1)
        {
            string[] choices = potions.ConvertAll(x => x.Name).ToArray();
            int choice = Game.Instance.ProcessChoice(choices);
            Consumable chosenConsumable = potions[choice];
            base.UseConsumable(chosenConsumable);
            RemoveFromInventory(chosenConsumable);
        }
        else
            Game.Instance.Tale("There are no Potions in your inventory...");
    }

    //public override bool UseShield()
    //{
    //    switch (Game.Instance.ProcessChoice(new string[] { "YES", "NO" }, "Use the Shield?"))
    //    {
    //        case 0:
    //            Game.Instance.Tale("The shield softens the blow.");
    //            return true;
    //        default: return false;
    //    }
    //}

    [System.Serializable]
    public class MySaveData : SaveData
    {
        public int CurrentMana { get; set; }
        public List<Skill.SaveData> Spellbook { get; set; }

    }
    public override SaveData GetSaveData()
    {
        SaveData saveData = base.GetSaveData();
        return new MySaveData
        {
            // Get generic character informations
            Stats = saveData.Stats,
            CurrentHP = saveData.CurrentHP,
            Equipment = saveData.Equipment,
            Inventory = saveData.Inventory,

            // Adding game-specific character informations
            Spellbook = Spellbook.ConvertAll(x => x.GetSaveData()),
            CurrentMana = CurrentManaPoints            
        };
    }

}

public abstract class Enemy : MyCharacter
{
    public Enemy(Dictionary<EquipSlot, Equipment> equip = null, List<Spell> spellbook = null) : base()
    {
        if (equip != null)
            foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
                Equip(e.Value, silent: true);
        //else
        //    equipment[EquipSlot.Weapon] = NaturalWeapon;

        if (spellbook != null)
            this.spellbook = spellbook;
    }

    public override string LogAction(string actionName)
    {
        return Name + " " + actionName + "s";
    }

    protected override string[] GetBattleActions()
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

    //public override bool UseShield()
    //{
    //    Random rnd = new Random();
    //    if (rnd.Next(0, 99) < 80)
    //    {
    //        Game.Instance.Tale(Name + " protects himself with a shield.");
    //        return true;
    //    }                
    //    else return false;
    //}

    //protected override void ChooseSpell(Character target)
    //{
    //    throw new NotImplementedException();
    //}

    //protected override void DrinkPotion()
    //{
    //    throw new NotImplementedException();
    //}
}

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

public class Soldier : Enemy
{
    public override string Name => "Soldier";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;
    public override Spell.ElementType ElementResistance => Spell.ElementType.Earth;

    public Soldier(Dictionary<EquipSlot, Equipment> equip, List<Spell> spellbook = null) : base(equip, spellbook)
    {
        MaxHP.SetBaseValue(50);
        Attack.SetBaseValue(10);
        Defense.SetBaseValue(7);
        Speed.SetBaseValue(10);
    }
}

public class Wolf : Enemy
{
    public override string Name => "Wolf";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;
    public override Spell.ElementType ElementResistance => Spell.ElementType.Air;

    public Wolf()
    {
        MaxHP.SetBaseValue(60);
        Attack.SetBaseValue(30);
        Defense.SetBaseValue(25);
        Speed.SetBaseValue(15);
        NaturalWeapon = new NaturalWeapon(rndRange: 25, precision: 80);
    }
}

public class GiantWasp : Enemy
{
    public override string Name => "Giant Wasp";
    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;
    public override Spell.ElementType ElementResistance => Spell.ElementType.Earth;

    public GiantWasp()
    {
        MaxHP.SetBaseValue(50);
        Attack.SetBaseValue(40);
        Defense.SetBaseValue(10);
        Speed.SetBaseValue(20);
        NaturalWeapon = new NaturalWeapon(rndRange: 15, precision: 66);
    }
}
#endregion

#region MyActions
public class WeaponAttack : Skill
{
    public override string Name => "Weapon Attack";

    public override void Effect(Character user, Character target)
    {
        Weapon weapon = user.ActiveWeapon;
        string attackLog = user.LogAction("attack") + " " + target.Name;
        if (!(weapon is NaturalWeapon)) { attackLog += " with " + weapon.Name; }
        Game.Instance.Tale(attackLog);

        // Random rolls
        Random rnd = new Random();
        int hitRoll = rnd.Next(1, 100);
        bool hit = hitRoll >= 100 - weapon.Precision + target.Speed.Value;
        bool critical = hitRoll >= (100 - weapon.Critical);

        if (hit)
        {
            // Damage calculations
            int attackValue = rnd.Next(user.Attack.Value - weapon.RndRange, user.Attack.Value + weapon.RndRange + 1);
            if (!critical) { attackValue -= target.Defense.Value; }
            int damage = PhysicalDamage(attackValue, weapon.DmgType, target.ActiveWeapon.Weakness);
            if (critical) { damage *= 3; Game.Instance.Tale("CRITICAL HIT!"); } // Apply critical bonus

            // Events call
            target.OnGetHit(damage);
            weapon.OnHit(user, target);
            weapon.Use(user);
        }
        else { Game.Instance.Tale("The attack misses..."); }
    }

    public static int PhysicalDamage(int atkDamage, Weapon.DamageType userType, Weapon.DamageType targetWeakness)
    {
        bool weaponAdvantage = targetWeakness == userType;
        //bool useShield = false;            
        if (weaponAdvantage)
        {
            Game.Instance.Tale("Weapon Advantage!"); // Alert for weapon advantage
            atkDamage = (int)(atkDamage * 1.5f); // Apply weapon weakness

            // Ask to use the shield if target has a shield
            //if (target.equipment.ContainsKey(EquipSlot.Shield))
            //    useShield = target.UseShield();
            //if (useShield)
            //    target.equipment[EquipSlot.Shield].WearOut();
            //else
            //    atkDamage = (int)(atkDamage * 1.5f); // Apply weapon weakness
        }
        return atkDamage;
    }
}

public abstract class Spell : Skill
{
    public enum ElementType { Earth, Water, Fire, Air, None }
    public abstract int Damage { get; }
    public abstract ElementType Element { get; }
    public virtual int ManaCost => 1;

    public override void Effect(Character user, Character target)
    {
        // Cast spell
        MyCharacter myTarget = (MyCharacter)target;
        Game.Instance.Tale(user.LogAction("cast") + " the spell: " + Name);
        int damage = MagicDamage(Damage, Element, myTarget.ElementWeakness, myTarget.ElementResistance);
        target.TakeDamage(damage);
        //base.Effect(user, target);
    }

    public static int MagicDamage(int spellDamage, Spell.ElementType damageElement, Spell.ElementType targetWeakness, Spell.ElementType targetResistance)
    {
        if (targetWeakness == damageElement) // Apply element weakness
        {
            spellDamage *= 2;
            Game.Instance.Tale("Element Weakness!");
        }
        else if (targetResistance == damageElement)
        {
            spellDamage /= 2;
            Game.Instance.Tale("Element Resistance!");
        }
        return spellDamage;
    }
}

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
    //public override Action<Character, Character> CustomEffects => (user, target) =>
    //{
    //    Game.Instance.Tale("Custom Effects for user: " + user.Name);
    //    Game.Instance.Tale("Custom Effects for target: " + target.Name);
    //};
}
#endregion

#region MyItems
public class RustySword : Sword
{
    public override string Name => "Rusty Sword";
    public override int Value => 3;
    public override int Attack => 20;
    public override int Durability => 10;
}

public class SteelSword : Sword
{
    public override string Name => "Steel Sword";
    public override int Value => 10;
    public override int Attack => 25;
    public override int Durability => 20;
}

public class Katana : Sword
{
    public override string Name => "Katana";
    public override int Value => 30;
    public override int Attack => 40;
    public override int Durability => 25;
}

public class Dagger : Sword
{
    public override string Name => "Dagger";
    public override int Value => 5;
    public override int Attack => 10;
    public override int RndRange => 5;
    public override int Precision => 100;
    public override int Weight => 0;
    public override int Durability => 20;
}

public class Claymore : Sword
{
    public override string Name => "Claymore";
    public override int Value => 50;
    public override int Attack => 40;
    public override int Weight => 7;
    public override int Durability => 20;
}

public class ThunderBlade : Sword
{
    public override string Name => "Thunder Blade";
    public override int Value => 350;
    public override int Attack => 30;
    public override int Weight => 2;
    public override int Durability => 10;
    public override Action<Character, Character> CustomEffects => (user, target) =>
    {
        Game.Instance.Tale("A thunder strikes " + target.Name + "!");
        MyCharacter myTarget = (MyCharacter)target;
        target.TakeDamage(
            Spell.MagicDamage(20, Spell.ElementType.Air, myTarget.ElementWeakness, myTarget.ElementResistance),
            "Magic "
            );
    };
}

public class ShortSpear : Spear
{
    public override string Name => "Short Spear";
    public override int Value => 7;
    public override int Attack => 20;
    public override int Durability => 20;
}

public class Club : Hammer
{
    public override string Name => "Club";
    public override int Value => 1;
    public override int Attack => 25;
    public override int Durability => 10;
}

public class MorningStar : Hammer
{
    public override string Name => "Morning Star";
    public override int Value => 40;
    public override int Attack => 35;
    public override int Weight => 3;
    public override int Durability => 20;
}

public abstract class Axe : Weapon
{
    public override DamageType DmgType => DamageType.Slashing;
    public override DamageType Weakness => DamageType.Slashing;
    public override int RndRange => 25;
    public override int Precision => 80;
    public override int Critical => 20;
    public override int Weight => 4;
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
    public override int Critical => 10;
    public override int Weight => 1;
    public override void OnEquip(Character character)
    {
        base.OnEquip(character);
        character.Defense.SetModifier(-10, this);
        character.Speed.SetModifier(10, this);
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
    public override int Defense => 5;
    public override int Weight => 1;
}

public abstract class Vest : BodyArmor
{
    public override int Defense => 0;
    public abstract int ManaBonus { get; }
    public override int Weight => 0;
    public override void OnEquip(Character character)
    {
        base.OnEquip(character);
        MyCharacter myChar = (MyCharacter)character;
        myChar.MaxManaPoints.SetModifier(ManaBonus, this);
        myChar.CurrentManaPoints += ManaBonus;
    }
}

public class BlueVest : Vest
{
    public override string Name => "Blue Vest";
    public override int Value => 5;
    public override int ManaBonus => 2;
}

public class WoodenShield : Shield
{
    public override string Name => "Wooden Shield";
    public override int Value => 5;
    public override int Defense => 6;
    public override int Durability => 3;
}

public class IronShield : Shield
{
    public override string Name => "Iron Shield";
    public override int Value => 20;
    //public override int Defense => 15;
    public override int Durability => 6;
}

public class PowerNecklace : Accessory
{
    public override string Name => "Power Necklace";
    public override int Value => 100;

    public override void OnEquip(Character character)
    {
        base.OnEquip(character);
        character.MaxHP.SetModifier(30, this);
        character.Attack.SetModifier(15, this);
    }
}

public class Potion : Consumable
{
    public override string Name => "Potion";
    public override int Value => 30;
    public override int Weight => 0;
    protected override void Effect(Character user)
    {
        user.CurrentHP = user.MaxHP.Value;
        Game.Instance.Tale(user.Name + " HP: " + user.CurrentHP);
    }
}

public class Elixir : Potion
{
    public override string Name => "Elixir";
    public override int Value => 70;
    public override int Weight => 0;
    protected override void Effect(Character user)
    {
        // Here custom elixir effects
    }
}
#endregion

#region MyMusic
public class IntroTheme : Music
{
    public override Note[] Tune => new Note[]
    {
        new Note(Tone.B, Duration.QUARTER),
        new Note(Tone.A, Duration.QUARTER),
        new Note(Tone.GbelowC, Duration.QUARTER),
        new Note(Tone.A, Duration.QUARTER),
        new Note(Tone.B, Duration.QUARTER),
        new Note(Tone.B, Duration.QUARTER),
        new Note(Tone.B, Duration.HALF),
        new Note(Tone.A, Duration.QUARTER),
        new Note(Tone.A, Duration.QUARTER),
        new Note(Tone.A, Duration.HALF),
        new Note(Tone.B, Duration.QUARTER),
        new Note(Tone.D, Duration.QUARTER),
        new Note(Tone.D, Duration.HALF)
    };
}
#endregion

#region Followers
public abstract class Follower : MyCharacter
{
    private int assistPoints;
    public abstract int MaxAssistPoints { get; }

    public Follower()
    {
        assistPoints = MaxAssistPoints;
    }

    public void AssistAttack(Character opponent)
    {
        // Check for enough assist points
        if (assistPoints > 0) assistPoints -= 1;
        else { Game.Instance.Tale("Not enough assist points..."); return; }

        new WeaponAttack().Effect(this, opponent);
    }

    public void Restore()
    {
        assistPoints = MaxAssistPoints;
    }

    public override void ChooseBattleAction(Character opponent)
    {
        throw new NotImplementedException();
    }
    protected override string[] GetBattleActions()
    {
        throw new NotImplementedException();
    }
    public override string LogAction(string actionName)
    {
        return Name + " " + actionName + "s";
    }
}

public class Juno : Follower
{
    public override string Name => "Juno";

    public override Spell.ElementType ElementWeakness => Spell.ElementType.Fire;

    public override Spell.ElementType ElementResistance => Spell.ElementType.Earth;

    public override int MaxAssistPoints => 2;

    public class WindAttack : NaturalWeapon
    {
        public WindAttack(int rndRange, int precision) : base(rndRange, precision) { }

        public override Action<Character, Character> CustomEffects => (user, target) =>
        {
            MyCharacter myTarget = (MyCharacter)target;
            target.TakeDamage(
                Spell.MagicDamage(100, Spell.ElementType.Air, myTarget.ElementWeakness, myTarget.ElementResistance),
                "Magic ");
        };
    }

    public Juno()
    {
        Attack.SetBaseValue(60);
        NaturalWeapon = new WindAttack(rndRange: 20, precision: 90);
    }
}
#endregion


////////////////////////////////////////////////
// CREATE GAME
////////////////////////////////////////////////
class MyGame : Game
{
    public MyCharacter myPlayer = new MyCharacter();
    protected MyCharacter Player => myPlayer;
    //protected virtual MyCharacter MyPlayer => (MyCharacter)Player;
    protected override string StartScene => "Intro"; // Should be "Intro" for game to start from the beginning; For debug purposes better change it in the savegame.json file

    // HERE YOUR CUSTOM WAY TO OUTPUT TEXT
    //public override void Tale(string text, bool stop = true)
    //{
    //    Console.WriteLine("CIAO");
    //    base.Tale(text, stop);
    //}
    [System.Serializable]
    public class SaveGame
    {
        public Game.SaveData GameData { get; set; }
        public MyCharacter.MySaveData PlayerData { get; set; }
    }

    protected override void Save(string fileName)
    {
        string jsonString = JsonSerializer.Serialize(new SaveGame()
        {
            GameData = GetSaveData(),
            PlayerData = (MyCharacter.MySaveData)Player.GetSaveData()
        });
        File.WriteAllText(fileName, jsonString);
    }

    protected override void Load(string fileName)
    {
        // Read save file
        string jsonString = File.ReadAllText(fileName);
        SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(jsonString);

        // Apply changes
        sceneToLoad = saveGame.GameData.SceneToLoad;
        achievements = saveGame.GameData.Achievements;
        Player.CurrentHP = saveGame.PlayerData.CurrentHP;
        Player.CurrentManaPoints = saveGame.PlayerData.CurrentMana;
        for (int i = 0; i < Player.stats.Count(); i++)
        {
            Player.stats[i].SetBaseValue(saveGame.PlayerData.Stats[i].BaseValue);
        }
        foreach (Item.SaveData itemData in saveGame.PlayerData.Inventory)
        {
            Item item = Activator.CreateInstance(Type.GetType(itemData.Type)) as Item;
            item.Uses = itemData.Uses;
            Player.AddToInventory(item, silent: true);
        }
        foreach (Item.SaveData equipData in saveGame.PlayerData.Equipment)
        {
            Equipment equip = Activator.CreateInstance(Type.GetType(equipData.Type)) as Equipment;
            equip.Uses = equipData.Uses;
            Player.Equip(equip, silent: true);
        }
        foreach (Skill.SaveData spellData in saveGame.PlayerData.Spellbook)
        {
            Spell spell = Activator.CreateInstance(Type.GetType(spellData.Type)) as Spell;
            myPlayer.LearnSpell(spell, silent: true);
        }
    }

    void BattleTest()
    {
        // myPlayer settings
        myPlayer.Equip(new RustySword());
        //myPlayer.Equip(new WoodenShield());
        myPlayer.Equip(new BlueVest());
        myPlayer.LearnSpell(new FireBall());
        //myPlayer.LearnSpell(new Meteor());
        myPlayer.AddToInventory(new Potion(), silent: true);
        myPlayer.AddToInventory(new Potion(), silent: true);
        myPlayer.AddToInventory(new Elixir(), silent: true);
        //myPlayer.AddToInventory(new Potion(), silent: true);
        //myPlayer.AddToInventory(new Potion(), silent: true);
        //myPlayer.AddToInventory(new Potion(), silent: true);
        myPlayer.AddToInventory(new ThunderBlade(), silent: true);

        // Assistants test
        myPlayer.followers.Add(new Juno());

        // Test enemy
        //Enemy testEnemy = new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
        //{
        //    { Character.EquipSlot.Weapon, new ShortSpear() },
        //    { Character.EquipSlot.Shield, new WoodenShield() }
        //});
        Enemy testEnemy = new Wolf();

        //myPlayer.FindEquipment(new WoodenShield());
        //myPlayer.FindEquipment(new Club());

        // Battle
        Battle testBattle = new Battle(Player, testEnemy);
        Character winner = testBattle.SingleFight();
        if (winner == Player)
            Tale("YOU WIN");
        else
            Tale("YOU LOSE");

        // Restore new myPlayer
        myPlayer = new MyCharacter();
    }

    void Rest()
    {
        // Restore Health
        myPlayer.CurrentHP = myPlayer.MaxHP.Value;

        // Save current scene
        Save(SavePath);

        // Reset followers (restore assist points, ecc)
        foreach (Follower follower in myPlayer.followers)
            follower.Restore();

        // Feedback
        Tale("You have recovered your energy.");
        Tale("You saved the game.");        
    }

    void UpgradeStat()
    {
        Tale("What do you want to improve?");
        switch (ProcessChoice(new string[]
            {
                "Attack",
                "Defense",
                "Speed",
                "Stamina",
                "Mana",
            }))
        {
            case 0: myPlayer.Attack.ChangeBaseValue(10); break;
            case 1: myPlayer.Defense.ChangeBaseValue(10); break;
            case 2: myPlayer.Speed.ChangeBaseValue(10); break;
            case 3: myPlayer.MaxHP.ChangeBaseValue(50); break;
            case 4: myPlayer.MaxManaPoints.ChangeBaseValue(1); break;
        }
    }

    protected override void MainMenu()
    {
    StartMenu:
        //new Thread(() => new IntroTheme()).Start();
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
                        case 1: goto StartMenu;
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
                    goto StartMenu;
                }
                break;
            case 2:
                Tale("This game was realized by Sprintingkiwi: https://github.com/sprintingkiwi/TextRPG");
                Tale("You can use this game library TextRPG.cs to program your own text RPG game");
                goto StartMenu;
            case 3: Environment.Exit(0); break;
            case 4:
                BattleTest();
                goto StartMenu;
        }
    }
        
    protected override void Initialize()
    {
        ////////////////////////////////////////////////
        // SETUP myPlayer
        ////////////////////////////////////////////////  
        myPlayer.Equip(new RustySword(), silent: true);
        //myPlayer.LearnSpell(new FireBall());
        myPlayer.MaxManaPoints.SetBaseValue(3);
        myPlayer.CurrentManaPoints = 2;
        //myPlayer.LearnSpell(waterSurge);
        //myPlayer.AddToInventory(new Club());
        //myPlayer.AddToInventory(new BlueVest());
        //myPlayer.AddToInventory(new Potion());
        myPlayer.GetItemsFromInventory<Potion>();
        //myPlayer.AddToInventory(new ShortSpear());
        //Console.WriteLine(myPlayer.Attack.Value);
        //Console.WriteLine(myPlayer.stats.Length);
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
            Tale("Your name is...");
            Console.WriteLine("YOU CAN EDIT YOUR NAME. LEAVE BLANK = 'Kros'");
            Player.Name = Console.ReadLine(); Tale("..." + Player.Name);
            Tale("A gentle female voice whispers something. A voice you are well used to hear.");           
            Tale("JUNO: There there, " + Player.Name + ", you'll finally eat some food and get some rest. We have the money to rent a room.");
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
                "Rest",
                "Talk with the innkeeper",
                "Read the quest board",                
            }))
            {
                case 0: myPlayer.ShowStatus(); break;
                case 1: Rest(); break;
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
            Tale("You look at the face of a middle-aged man, with great mustaches and a bald head.");
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

            if (achievements.Contains("Spellcaster"))
                return "Cave Terror";

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
            myPlayer.LearnSpell(new FireBall());

            achievements.Add("Spellcaster"); // The myPlayer passed the tutorial
            Save(SavePath); // We can save the game because the myPlayer will be able to go back to the Inn anyway

            return "Cave Terror";
        }));


        AddScene(new Scene("Cave Terror", () =>
        {
            switch (ProcessChoice(new string[]
            {
                "Go back",
                "Move on"
            }))
            {
                case 0: return "Four Graves Inn";
                case 1: break;
            }

            Tale("As you move forward, deeper into the cave, you hear the sound of fast steps approaching...");
            Tale("IT'S A GOBLIN!");
            Tale("And he's not friendly. The Goblin attacks you!");
            Tale("JUNO: That Goblin is armed with a club!");
            Tale("JUNO: With your sword you should have a weapon advantage on him.");

            myPlayer.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
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
                    myPlayer.AddToInventory(new ShortSpear());
                    Tale("Inside the bag you find two Potions!");
                    Tale("JUNO: Great! A Potion can be a life saver.");
                    myPlayer.AddToInventory(new Potion());
                    myPlayer.AddToInventory(new Potion());
                    break;
                case 1:
                    Tale("You walk down a narrow corridor. Many raw Clubs are piled up agains the walls.");
                    Tale("Also, a worn out chest lies in corner, filled with spider webs.");
                    Tale("JUNO: You should take one of those Clubs, it could be useful against certain foes.");
                    myPlayer.AddToInventory(new Club());
                    Tale("JUNO: ...And let's check that chest. This seems to be the Goblins armory and that might be their treasure vault.");
                    Tale("You cut the spider webs with your sword and open the chest: there is a Potion inside!");
                    Tale("JUNO: Great! A Potion can be a life saver.");
                    myPlayer.AddToInventory(new Potion());
                    break;
            }

            Tale("GAAAAAAAAAAAAAAAA!");
            Tale("Suddenly you hear a scream. The voice of a young woman...");
            Tale("JUNO: It's the kindapped girl! We must move forward.");
            Tale("You move into a larger room. There's wooden cage: it's opened, and inside you see a scared mud-covered girl, crying as loud as she can.");
            Tale("Before her, another Goblin. As soon as you get closer, the Goblin smells you and turns to you with an angry grin on his face.");
            Tale("JUNO: Watch out Kros! This Goblin is equipped with a Spear...");
            Tale("JUNO: You should change your weapon as soon as the battle starts, or you will suffer a weapon disadvantage!");
            
            myPlayer.Battle(new Goblin(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new ShortSpear() }
            }));
            
            Tale("After the defeated Goblin exhales his last breath, you hurry to free the poor girl.");
            Tale("YOU: It's alright girl, your mother sent me here to rescue you.");
            Tale("The trembling girl stands still, confused, but also looking with a spark of curiosity that figure...");
            Tale("... Of a man with a hood and a long cloak, shrouded in the darkness of the cave.");
            Tale("JUNO: Hush, little jewel, you are safe now...");
            Tale("The girl slowly tries to wipe away the tears with the back of his hands.");
            Tale("GIRL: Who are you? Where did this voice came from?");
            Tale("JUNO: You can't see me now, I am an air elemental.");
            Tale("And saying so, a gentle breeze moves the girl's hair.");
            Tale("GIRL: Air elemental?");
            Tale("YOU: We must hurry out of this place. Can you walk on your feet?");
            Tale("GIRL: Yes...");
            Tale("JUNO: Let's move. We don't know how many Goblins could still be nearby...");           

            Tale("As the three of you approach the much coveted exit of the cave, you suddenly feel an unsettling presence.");
            Tale("You turn back and see two red eyes glowing in that darkenss you were about to leave behind...");
            Tale("JUNO: A Wolf!");
            Tale("JUNO: This Wolf is a fearsome opponent... You might not be able to defeat him with the weapons strenght alone!");
            Tale("JUNO: This could be the very good moment to try out that Fire Ball spell, don't you think?");
            if (myPlayer.CurrentManaPoints < 2)
            {
                Tale("JUNO: I see you already consumed your mana, the magical energy that enables you to cast spells.");
                Tale("JUNO: For this time I will gift you with a bit of my energy...");
                myPlayer.CurrentManaPoints = 2;
                Tale("JUNO: ...But remember, I won't always be able to do this!");
            }

            myPlayer.Battle(new Wolf());

            achievements.Add("Hero");                        
            return "After Cave Terror";
        }));

        AddScene(new Scene("After Cave Terror", () =>
        {
            Tale("JUNO: Kros! Watch out!");
            Tale("The giant black Wolf jumps towards you with every remnant of its strenght.");
            Tale("YOU: Fire... Ball!");
            Tale("The little scared girl stands amazed while a burst of flames flashes out of your hands.");
            Tale("A burning sphere engulfs the Wolf mid air, crushing the beast against the cave wall.");
            Tale("Its lifeless body now lying on the ground...");
            Tale("For a brief moment, no one talks. Only silence, and the smell of roasted meat and burnt fur.");
            Tale("JUNO: Well done, Kros.");
            Save(SavePath);
            Tale("---");
            Tale("The party then heads out of the cave, walking down the path that leads to the Four Graves Inn.");
            Tale("After nearly an hour of walk, the inn could now be seen in the distance.");
            Tale("GIRL: My house is on the road, there, near that little wheat field...");
            Tale("As you approach an old wooden house, the girl recognizes a woman that was praying on her kneel, right after a tombstone.");
            Tale("Her long black dress moved by a strong wind coming from the hills.");
            Tale("GIRL: Mother!");
            Tale("As soon as they see each others, they run to meet and hug.");
            Tale("WOMAN: Lea!");
            Tale("GIRL: Mother! I was so scared!");
            Tale("Says the girl, crying.");
            Tale("WOMAN: I knew you were still alive. I prayed all the day on the tomb of you father. He protected you...");
            Tale("Then the girl turns towards you.");
            Tale("LEA: And this man saved me. He fought all the Goblins that were keeping me captive.");
            Tale("WOMAN (Looking at you): I don't know how to thank you... We don't have much but we will gladly share our food with you, and you could stay under our roof as long as you need.");
            Tale("A smile of joy makes its way on your face. You are not used to such kindness.");
            Tale("JUNO: Kros.");
            Tale("YOU: I know. We cannot stay.");
            Tale("As you think of something to answer, a strong gust of wind pulls your hood down,");
            Tale("revealing two blood-red horns emerging from your black hair, and the unusually dark olive skin of your face");
            Tale("At this sight, the woman withdraws, pulling her daughter to her arms.");
            Tale("WOMAN: A monster!");
            Tale("Their eyes now trembling with fear...");
            Tale("WOMAN: Please, stay away from us!");
            Tale("Then the two hurry in the house, barring the door from the inside.");
            Tale("JUNO: Let's go,");
            Tale("JUNO: Kros");

            //Tale("THIS DEMO ENDS HERE.");
            //gameOver = true;
            return "To Snake Village";
        }));
        #endregion

        #region Chapter 1
        AddScene(new Scene("To Snake Village", () =>
        {
            Rest();

            Tale("You are walking deep inside a forest, which progressively gives way to a swamp.");
            Tale("Purple lights shining on the vegetation and strange insects buzzing around.");
            Tale("The feeling that magic is floating in the air...");
            Tale("YOU: Juno, remind me why we are taking this path...");
            Tale("JUNO: The Sage told us to find an Naga Priestess.");
            Tale("YOU: Are you sure that this is the way?");
            Tale("JUNO: I am. Naga people always live deep into the mystical swamps.");
            Tale("After a little more walking, the path climbs up a small hill and you catch a glimpse of an engraved ideogram held up on a big wooden portal.");
            Tale("You don't have the time to ask Juno more informations about it that you feel the cold of steel on your throat...");
            Tale("... And see a curved blade poking out of the surrounding vegetation!");
            Tale("You suddenly freeze! Slowly, a stealthy shape slithers onward from behind a bramble.");
            Tale("A man with almond eyes and a pale light-green skin looks at you with a grim face.");
            Tale("Black hair carefully gathered in a topknot above his head.");
            Tale("A long snake-like body stood in place of his legs from the waist down.");
            Tale("NAGA WARRIOR: Identify yourself!");
            Tale("JUNO: Honorable Naga warrior. We are headed to your swamp village.");
            Tale("As Juno started to talk, a feeble wind moved the snake-man's vest under his wooden armor, while he started to look in every direction trying to figure out where the voice was coming from.");
            Tale("I am Juno, an Air Elemental, and this is Kros, one of the last Kuraktais.");
            Tale("You take off your hood to show your face and, above all, your horns.");
            Tale("NAGA WARRIOR: A Kuraktai!");
            Tale("The Naga suddenly lowers his sword, sheathing it with an elegant gesture. Then he slightly bows to you.");
            Tale("NAGA WARRIOR: Forgive me. A noble Kuraktai is more than welcome in our swamps.");
            Tale("NAGA WARRIOR: My name is Kazuto. Follow me, I will lead you to the village.");
            Tale("---");
            Tale("After some more walk, the Naga suddenly freeze.");
            Tale("KAZUTO: Do you hear this bug buzz? They're getting closer.");
            Tale("YOU: What's getting closer?");
            Tale("KAZUTO: Giant Wasps, they're predators in these swamps. Prepare to fight!");
            Tale("As the buzz gets louder, four Giant Wasps with bloodshot eyes emerge surrounding the party.");
            Tale("KAZUTO: I'll take the two on the left, you take the two on the right!");
            myPlayer.Battle(new GiantWasp());
            Tale("You take down the first wasp, but the other one is still attacking you!");
            myPlayer.Battle(new GiantWasp());
            Tale("KAZUTO: They get more aggressive day after day... There must be something that's pushing them out of their territory.");
            Tale("---");

            return "Snake Village";
        }));


        AddScene(new Scene("Snake Village", () =>
        {
            Tale("You follow the Naga Warrior through the swamp, alongside your companion Juno.");
            Tale("After half an hour of walking, you reach the heart of the Naga village: an island inside a lake, surrounded by a wooden palisade.");
            Tale("As you walk among the Naga folks with your hood down, you can hear murmurs of surprise and admiration.");
            Tale("NAGA VILLAGER: That's... a real Kuraktai...");
            Tale("Finally you reach the center of the village. A large building made of intertwined bamboo and plants stands before you.");
            Tale("KAZUTO: We are here, the Priestess will receive you inside.");
            Tale("You enter the door moving a curtain of soft silk. Inside, a sinuous figure with long dark hair stands on her snake-like tail, giving you her back.");
            Tale("PRIESTESS: Come forward.");
            Tale("PRIESTESS: Come, Kros son of Unk, heir of the Kuraktais.");
            Tale("YOU: Greetings... your... your majesty...");
            Tale("Slowly the woman turns around, showing a beautiful face painted with tribal symbols.");
            Tale("PRIESTESS: Haha... I'm not a Majesty. I'm just a Naga woman, with a deep connection to Nature.");
            Tale("YOU: How did you know my name?");
            Tale("PRIESTESS: Time and space are linked, they are bound to Nature, and sometimes I can catch a glimpse of what has yet to happen.");
            Tale("PRIESTESS: I was waiting for you. We will need your help soon enough.");
            Tale("PRIESTESS: And I was waiting you too, Elemental.. I can see you have a bound with this boy, just as you had a bound with his father...");
            Tale("JUNO: I merely hope to be a guidance for him.");
            Tale("YOU: We were...");
            Tale("NAGA GUARD: Priestess!!! ");
            Tale("NAGA GUARD: Priestess!!! It's the humans!");
            Tale("NAGA GUARD: A whole battalion of human soldiers is heading towards us.");
            Tale("PRIESTESS: Lift the bridges. Defend the village!");
            Tale("PRIESTESS: Your destiny unfolds, Kros.");
            Tale("KAZUTO: I see you don't have much of an equipment.");
            Tale("KAZUTO: Here, take this sword. It's Naga craftmanship.");
            myPlayer.FindEquipment(new Katana());
            Tale("KAZUTO: And these medicines too.");
            myPlayer.AddToInventory(new Potion());
            myPlayer.AddToInventory(new Potion());
            Tale("You rush outside along with Kazuto. You see Naga warriors running towards the borders of the village, trying to help every villager to get inside the palisade.");
            Tale("KAZUTO: Most of the enemies are storming the eastern front. Our warriors are well trained. I'm sure they'll take care of them.");
            Tale("KAZUTO: Come with me, we will guard the western front. Other humans might be attacking from there.");
            Tale("You follow Kazuto beyond the palisade, while a bunch of other Naga warriors join your group.");
            Tale("You are startled by screams of battle from the opposite side of the village.");
            Tale("JUNO: Be brave, Kros. You path will often lead to battle... and being overwhelmed by fear would lead you to certain death.");
            Tale("At first, nothing moves. But then a shadow moving behind a tree. Sounds of steps approaching.");
            Tale("KAZUTO: A trap!");
            Tale("KAZUTO: The attack from the front was a diversion! Gather men here!");
            Tale("KAZUTO: Kros, be careful and stand by my side!");

            return ("Naga Battle");
        }));

        AddScene(new Scene("Naga Battle", () =>
        {
            Save(SavePath);
            Tale("More than 20 human warriors surround your group.");

            switch (ProcessChoice(new string[]
            {
                "Flee",
                "Stand"
            }))
            {
                case 0:
                    Tale("You try to run away from the battle, but some enemy soldiers intercept you...");
                    myPlayer.Battle(new Soldier(new Dictionary<Character.EquipSlot, Equipment>()
                    {
                        { Character.EquipSlot.Weapon, new ShortSpear() },
                    }));
                    myPlayer.Battle(new Soldier(new Dictionary<Character.EquipSlot, Equipment>()
                    {
                        { Character.EquipSlot.Weapon, new SteelSword() },
                    }));
                    Tale("Kros! Don't get too far from me or I won't be able to defend you!");
                    break;

                case 1:
                    achievements.Add("Stand battle ground");
                    Tale("Some enemy soldiers attack you!");                    
                    myPlayer.Battle(new Soldier(new Dictionary<Character.EquipSlot, Equipment>()
                    {
                        { Character.EquipSlot.Weapon, new HandAxe() },
                        { Character.EquipSlot.Body, new LeatherArmor() },
                    }));
                    Tale("The enemy dropped a Hand Axe!");
                    myPlayer.FindEquipment(new HandAxe());
                    myPlayer.Battle(new Soldier(new Dictionary<Character.EquipSlot, Equipment>()
                    {
                        { Character.EquipSlot.Weapon, new ShortSpear() },
                    }));
                    Tale("KAZUTO: Great work Kros! Now let me take care of these other thugs!");
                    break;
            }
            Tale("You see Kazuto waiting still as three more human soldiers advance, than he sprints forward and after three cuts of his sword the soldiers lay dead on the ground.");
            Tale("JUNO: Kros watch! Behind you!");
            Tale("Another soldier is charging in your direction, but before he can reach you he gets enveloped in a small tornado of leaves.");
            myPlayer.Battle(new Soldier(new Dictionary<Character.EquipSlot, Equipment>()
            {
                { Character.EquipSlot.Weapon, new Club() },
            }));
            Tale("YOU: Thanks, Juno. He could have taken me by surprise if you hadn't slowed him down.");

            Tale("As their number decreases, the last human soldiers alive begin to retreat.");
            if (achievements.Contains("Stand battle ground"))
                Tale("KAZUTO: Thanks Kros, you did a great work!");
            else
                Tale("KAZUTO: Are you all right?");
            Tale("KAZUTO: The cowards are retrating, the village is safe... for now.");

            return "Snake Village End";
        }));

        AddScene(new Scene("Snake Village End", () =>
        {
            Rest();

            Tale("You, Juno and Kazuto return inside the Priestess temple.");
            Tale("PRIESTESS: The Naga people thank you for fighting at our side, Kros son of Unk.");
            Tale("PRIESTESS: The two of you can be our guests as long as you need, if you wanna rest or heal some wound.");
            Tale("JUNO: We thank you so much, but Kros and I will depart right now.");
            Tale("JUNO: But before that, we need to ask you something. We are in search of an ancient magical artifact, the...");
            Tale("PRIESTESS: The Obsidian Sword, the Legendary weapon forged in ancient Kuraktai history.");
            Tale("YOU: Well yes but... How did you know? Well, never mind...");
            Tale("JUNO: We were told that a Naga Priestess might know something, since your people helped forging the Obsidian Sword.");
            Tale("PRIESTESS: Unfortunately, I can't tell you much abou...");
            Tale("Suddenly, the eyes of the priestess roll back and her chest starts to tremble.");
            Tale("PRIESTESS: Dark times are coming, chosen one!");
            Tale("PRIESTESS: The Flame Sword is only one of the Three Legendary Weapons you must find.");
            Tale("PRIESTESS: Your search will start North-West from here, in a place that has no sky...");
            Tale("PRIESTESS: A great evil is upon us. You must find the Three Weapons! You must duel with...");
            Tale("The priestess eyes get back to normal and her trembling stops.");
            Tale("PRIESTESS (clearing her voice): ...as I was saying, I can't tell you much about the sword.");
            Tale("YOU: Did you just...");
            Tale("JUNO: Lady Priestess, you had a vision!");
            Tale("PRIESTESS: A vision? What are you talking about?");
            Tale("JUNO: You just told us something, you were like... possessed?");
            Tale("KAZUTO (a little shaken): That's... That's right, My Lady...");
            Tale("PRIESTESS: Interesting. An unconscious vision. For what I know it never happened before.");
            Tale("PRIESTESS: Such a rare phenomenon, It only happens when a vision is very very powerful... What did I exactly say?");
            Tale("YOU: Something about dark times, three weapons that I should find and... Oh, and you called me 'chosen one'");
            Tale("PRIESTESS: I see.");
            Tale("JUNO: The three weapons? I only know about the existence of the Obsidian Sword. In my long life I never heard proof about the other two being more than just a tale.");
            Tale("PRIESTESS: They exist. I've seen all the three legendary weapons once, in my visions.");
            Tale("PRIESTESS: The Obsidian Sword.");
            Tale("PRIESTESS: The Sacred Lance.");
            Tale("PRIESTESS: The Calamity Hammer.");
            Tale("PRIESTESS: Three Weapons of Power, three legendary items, hidden in the world they are bound to save.");
            Tale("PRIESTESS: So you, Kros, must find them.");
            Tale("PRIESTESS: Humans killing elementals, ancient races becoming violent... The world has no balance now.");
            Tale("PRIESTESS: ");


            Tale("");
            return "";
        }));

        AddScene(new Scene("A new Journey", () =>
        {
            Rest();

            Tale("");
            return "";
        }));

        AddScene(new Scene("", () =>
        {
            Rest();            

            Tale("");
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
