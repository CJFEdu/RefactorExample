using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

[System.Serializable]
public class ArmAbilityGroup
{
    [SerializeField] public Growing reach;
    [SerializeField] public Strength strong;
    [SerializeField] public Electric shock;
    public Growing Reach
    {
        get
        {
            return reach;
        }
        set
        {
            reach = value;
        }
    }
    public Strength Strong
    {
        get
        {
            return strong;
        }
        set
        {
            strong = value;
        }
    }
    public Electric Shock
    {
        get
        {
            return shock;
        }
        set
        {
            shock = value;
        }
    }
}

public class AbilityManagerScript : S_InputReceiver
{
    [SerializeField] private EventBus OnAbilityActivation;
    public static AbilityManagerScript Instance { get; private set; }

    private int playerIndex = -1;
    public bool isShift = false;
    public bool leftWasAlt = false;
    public bool rightWasAlt = false;

    public delegate void ArmChange(object sender, EventArgs e);

    public event ArmChange LeftArmChanged;
    public event ArmChange RightArmChanged;

    public enum Ability
    {
        none, reach, strong, shock
    };

    public static Ability abilityLeft = Ability.none;
    public static Ability abilityRight = Ability.none;

    // [SerializeField] private IArmType reach;
    // [SerializeField] private IArmType strong;
    // [SerializeField] private IArmType shock;

    private List<IArmType> leftArmList = new List<IArmType>();
    private List<IArmType> rightArmList = new List<IArmType>();

    private bool[] AbilityLock = {false, false, false};

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            // Left arm first ability
            RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate, ReceiveInput.Pressed, execute);
            // Right arm first ability
            RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate, ReceiveInput.Pressed, execute);
            // Left arm second ability
            RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate2, ReceiveInput.Pressed, execute);
            // Right arm second ability
            RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate2, ReceiveInput.Pressed, execute);
            // Release events, same order
            RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate, ReceiveInput.Released, cancel);
            RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate, ReceiveInput.Released, cancel);
            RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate2, ReceiveInput.Released, cancel);
            RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate2, ReceiveInput.Released, cancel);
            // Use ability 2 on keyboard
            RegisterInput(EA_PlayerInputArguments.EInputType.Shift, ReceiveInput.Pressed, shiftOn);
            RegisterInput(EA_PlayerInputArguments.EInputType.Shift, ReceiveInput.Released, shiftOff);
            // Ability Switching
            RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmToggle, ReceiveInput.Pressed, nextLeft);
            RegisterInput(EA_PlayerInputArguments.EInputType.RightArmToggle, ReceiveInput.Pressed, nextRight);

        }
    }

    public void Init(ArmAbilityGroup LeftArm, ArmAbilityGroup RightArm)
    {
        leftArmList = new List<IArmType>();
        leftArmList.Add(null);
        leftArmList.Add(LeftArm.reach);
        leftArmList.Add(LeftArm.strong);
        leftArmList.Add(LeftArm.shock);
        rightArmList = new List<IArmType>();
        rightArmList.Add(null);
        rightArmList.Add(RightArm.reach);
        rightArmList.Add(RightArm.strong);
        rightArmList.Add(RightArm.shock);
    }

    // When Ability Used
    void execute(object sender, EA_PlayerInputArguments e)
    {
        //Debug.Log((int)e.PlayerInputType);
        var args = new AbilityActivationArgs();
        switch (e.PlayerInputType)
        {
            case EA_PlayerInputArguments.EInputType.LeftArmActivate:
                if (isShift)
                {
                    // If shifting run alt ability instead
                    if (abilityLeft != Ability.none)
                    {
                        // Deactivate other movement abilities
                        if (abilityRight != Ability.none) rightArmList[(int) abilityRight].Deactivate(true /*Right Arm 2*/);
                        leftArmList[(int) abilityLeft].Deactivate(true /*Left Arm 2*/);
                        // Activate new ability
                        leftArmList[(int) abilityLeft].Activate(true /*Left Arm 2*/);
                        args = new AbilityActivationArgs(abilityLeft, true, true);
                    }
                    leftWasAlt = true;
                }
                else if (abilityLeft != Ability.none)
                {
                    // Activate non-movement ability
                    leftArmList[(int) abilityLeft].Activate(false /*Left Arm 1*/);
                    args = new AbilityActivationArgs(abilityLeft, false, true);
                }
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate:
                if (isShift)
                {
                    // If shifting run alt ability instead
                    if (abilityRight != Ability.none)
                    {
                        // Deactivate other movement abilities
                        if (abilityLeft != Ability.none) leftArmList[(int) abilityLeft].Deactivate((true) /*Right Arm 2*/);
                        rightArmList[(int) abilityRight].Deactivate(true /*Left Arm 2*/);
                        // Activate new ability
                        rightArmList[(int) abilityRight].Activate(true /*Left Arm 2*/);
                        args = new AbilityActivationArgs(abilityRight, true, true);
                    }
                    rightWasAlt = true;
                    break;
                }
                else if (abilityRight != Ability.none)
                {
                    // Activate non-movement abiilty
                    rightArmList[(int) abilityRight].Activate(false /*Right Arm 1*/);
                    args = new AbilityActivationArgs(abilityRight, false, true);
                }
                break;
            case (EA_PlayerInputArguments.EInputType.LeftArmActivate2):
                if (abilityLeft != Ability.none)
                {
                    leftArmList[(int) abilityLeft].Activate(true /*Left Arm 2*/);
                    args = new AbilityActivationArgs(abilityLeft, true, true);
                }
                break;
            case (EA_PlayerInputArguments.EInputType.RightArmActivate2):
                if (abilityRight != Ability.none)
                {
                    rightArmList[(int) abilityRight].Activate(true /*Right Arm 2*/);
                    args = new AbilityActivationArgs(abilityRight, true, true);
                }
                break;
        }
        OnAbilityActivation.Trigger(this, args);
    }

    void cancel(object sender, EA_PlayerInputArguments e)
    {
        var args = new AbilityActivationArgs();
        switch (e.PlayerInputType)
        {
            case EA_PlayerInputArguments.EInputType.LeftArmActivate:
                if (abilityLeft != Ability.none)
                {
                    if (leftWasAlt)
                    {
                        leftWasAlt = false;
                        leftArmList[(int) abilityLeft].Deactivate(true /*Left Arm 2*/);
                        args = new AbilityActivationArgs(abilityLeft, true, false);
                    }
                    else
                    {
                        leftArmList[(int) abilityLeft].Deactivate(false /*Left Arm 1*/);
                        args = new AbilityActivationArgs(abilityLeft, false, false);
                    }
                }
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate:
                if (abilityRight != Ability.none)
                {
                    if (rightWasAlt)
                    {
                        rightWasAlt = false;
                        rightArmList[(int) abilityRight].Deactivate(true /*Right Arm 2*/);
                        args = new AbilityActivationArgs(abilityRight, true, false);
                    }
                    else
                    {
                        rightArmList[(int) abilityRight].Deactivate(false /*Right Arm 1*/);
                        args = new AbilityActivationArgs(abilityRight, false, false);
                    }
                }
                break;
            case EA_PlayerInputArguments.EInputType.LeftArmActivate2:
                if (abilityLeft != Ability.none)
                {
                    leftArmList[(int) abilityLeft].Deactivate(true /*Left Arm 2*/);
                    args = new AbilityActivationArgs(abilityLeft, true, false);
                }
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate2:
                if (abilityRight != Ability.none)
                {
                    rightArmList[(int) abilityRight].Deactivate(true /*Right Arm 2*/);
                    args = new AbilityActivationArgs(abilityRight, true, false);
                }
                break;
        }
        OnAbilityActivation.Trigger(this, args);
    }

    void shiftOn(object sender, EA_PlayerInputArguments e)
    {
        isShift = true;
    }

    void shiftOff(object sender, EA_PlayerInputArguments e)
    {
        isShift = false;
    }

    void nextLeft(object sender, EA_PlayerInputArguments e)
    {
        // Deactivate left arm abilities
        if (abilityLeft != Ability.none)
        {
            leftArmList[(int) abilityLeft].Deactivate(false /*Left Arm 1*/);
            leftArmList[(int) abilityLeft].Deactivate(true /*Left Arm 2*/);
        }
        // Switch Ability
        switch (abilityLeft)
        {
            case Ability.none:
                //Debug.Log("Changed Left to Reach");
                abilityLeft = Ability.reach;
                break;
            case Ability.reach:
                //Debug.Log("Changed Left to Strong");
                abilityLeft = Ability.strong;
                break;
            case Ability.strong:
                //Debug.Log("Changed Left to Shock");
                abilityLeft = Ability.shock;
                break;
            case Ability.shock:
                //Debug.Log("Changed Left to None");
                abilityLeft = Ability.none;
                break;
        }
        var args = new ArmChangeArgs(abilityLeft);
        LeftArmChanged?.Invoke(this, args);
    }

    void nextRight(object sender, EA_PlayerInputArguments e)
    {
        // Deactivate right arm abilities
        if (abilityRight != Ability.none)
        {
            rightArmList[(int) abilityRight].Deactivate(false /*Right Arm 1*/);
            rightArmList[(int) abilityRight].Deactivate(true /*Right Arm 2*/);
        }
        // Switch Ability
        switch (abilityRight)
        {
            case Ability.none:
                //Debug.Log("Changed Right to Reach");
                abilityRight = Ability.reach;
                break;
            case Ability.reach:
                //Debug.Log("Changed Right to Strong");
                abilityRight = Ability.strong;
                break;
            case Ability.strong:
                //Debug.Log("Changed Right to Shock");
                abilityRight = Ability.shock;
                break;
            case Ability.shock:
                //Debug.Log("Changed Right to None");
                abilityRight = Ability.none;
                break;
        }
        var args = new ArmChangeArgs(abilityRight);
        RightArmChanged?.Invoke(this, args);
    }

    int convertStringToIndex (String ability) {
        if (ability.ToLower() == "reach" || ability.ToLower() == "growing")
            return 0;
        if (ability.ToLower() == "strong" || ability.ToLower() == "strength")
            return 1;
        if (ability.ToLower() == "shock" || ability.ToLower() == "electric")
            return 2;
        return -1;
    }
    int convertEnumToIndex (Ability ability) {
        if (ability == Ability.reach)
            return 0;
        if (ability == Ability.strong)
            return 1;
        if (ability == Ability.shock)
            return 2;
        return -1;
    }

    public bool isUnlockedAbility (String ability) {
        return isUnlockedAbility(convertStringToIndex(ability));
    }

    public bool isUnlockedAbility (Ability ability) {
        return isUnlockedAbility(convertEnumToIndex(ability));
    }

    public bool isUnlockedAbility (int ability) {
        return AbilityLock[ability];
    }

    public void setLeftAbility (String ability) {
        setLeftAbility(convertStringToIndex(ability));
    }

    public void setRightAbility (Ability ability) {
        setLeftAbility(convertEnumToIndex(ability));
    }

    public void setLeftAbility (int ability) {
        Ability temp = Ability.none;
        if (ability == 0 && AbilityLock[0])
            temp = Ability.reach;
        if (ability == 1 && AbilityLock[1])
            temp = Ability.strong;
        if (ability == 2 && AbilityLock[2])
            temp = Ability.shock;
        if (temp != Ability.none)
            abilityLeft = temp;
    }

    public void setRightAbility (String ability) {
        setRightAbility(convertStringToIndex(ability));
    }

    public void setLeftAbility (Ability ability) {
        setRightAbility(convertEnumToIndex(ability));
    }

    public void setRightAbility (int ability) {
        Ability temp = Ability.none;
        if (ability == 0 && AbilityLock[0])
            temp = Ability.reach;
        if (ability == 1 && AbilityLock[1])
            temp = Ability.strong;
        if (ability == 2 && AbilityLock[2])
            temp = Ability.shock;
        if (temp != Ability.none)
            abilityRight = temp;
    }

    // Takes an ability name string and calls a function to lock/unlock that ability
    public void updateAbility (String ability, bool state) {
        updateAbility(convertStringToIndex(ability), state);
    }

    // Takes an ability enum state and calls a function to lock/unlock that ability
    public void updateAbility (Ability ability, bool state) {
        updateAbility(convertEnumToIndex(ability), state);
    }

    // locks/unlocks the ability at index ind of the AbilityLock array
    public void updateAbility (int ind, bool state) {
        if (ind != -1)
            AbilityLock[ind] = state;
    }
    
    public void updateAllAbilities (bool state) {
        for (int i = 0; i < AbilityLock.Length; i++)
        {
            AbilityLock[i] = state;
        }
    }
}

public class ArmChangeArgs : EventArgs
{
    public AbilityManagerScript.Ability Ability { get; set; }

    public ArmChangeArgs(AbilityManagerScript.Ability ability)
    {
        Ability = ability;
    }
}

public class AbilityActivationArgs : EventArgs
{
    public AbilityManagerScript.Ability Ability { get; }
    public bool IsMovement { get; }
    public bool Activated { get; }

    public AbilityActivationArgs()
    {

    }

    public AbilityActivationArgs(AbilityManagerScript.Ability ability, bool isMovement, bool activated)
    {
        Ability = ability;
        IsMovement = isMovement;
        Activated = activated;
    }
}
