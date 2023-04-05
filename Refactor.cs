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
}

public class ArmChangeArgs  EventArgs
{
    public Ability NewAbility { get; private set; }
    public AbilityManagerScript.Arm Arm { get; private set; }

    public ArmChangeArgs(AbilityManagerScript.Arm arm,Ability ability)
    {
        NewAbility = ability;
        Arm = arm;
    }
}

public class AbilityActivationArgs  EventArgs
{
    public Ability Ability { get; }
    public bool IsMovement { get; }
    public bool Activated { get; }
    
    public AbilityManagerScript.Arm Arm { get; private set; }

    public AbilityActivationArgs(AbilityManagerScript.Arm arm, Ability ability, bool isMovement, bool activated)
    {
        Ability = ability;
        IsMovement = isMovement;
        Activated = activated;
        Arm = arm;
    }
}

public class ArmContainer
{
    private IArmType[]  _armActionsOnPlayer;
    public Ability CurrentAbility = Ability.None;
    
    public ArmContainer(IArmType[]  armActionsOnPlayer)
    {
        _armActionsOnPlayer = armActionsOnPlayer;
    }

    public void StopActions()
    {
        if(CurrentAbility == Ability.None)
            return;
        _armActionsOnPlayer[(int)CurrentAbility].Deactivate(true);
        _armActionsOnPlayer[(int)CurrentAbility].Deactivate(false);
    }
    
    public IArmType GetArmAction(Ability ability)
    {
        if(ability == Ability.None)
            return null;
        return _armActionsOnPlayer[(int)ability];
    }
    
    public IArmType GetCurrentArmAction()
    {
        return GetArmAction(CurrentAbility);
    }
}

public class AbilityManagerScript  S_InputReceiver, IControlDataPersistence
{
    public static AbilityManagerScript Instance { get; private set; }
    
    public enum Arm
    {
        Left, Right
    }

    private bool _isShift = false;
    private bool[] _wasAlt = {false, false};

    public delegate void ArmChange(object sender, ArmChangeArgs e);
    
    public event ArmChange ArmAbilityChanged;
    
    public delegate void AbilityActivation(object sender, AbilityActivationArgs e);
    public event AbilityActivation ArmAbilityActivated;
    
    public enum Ability
    {
        Reach, Strong, Shock,None
    };
    
    public const Ability DefaultAbility = Ability.Reach;
    public const int AbilityCount = 3;
    public static readonly Ability[] AllAbilities = {Ability.Reach, Ability.Strong, Ability.Shock};


    public bool[] _abilityLock = {true, false, false};
    
    private DictionaryArm,ArmContainer _armReferences = new DictionaryArm, ArmContainer();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            if(AllAbilities.Length != AbilityCount)
                Debug.LogError(Ability count does not match the number of abilities in the enum);
            if((int)Ability.None != AbilityCount)
                Debug.LogError(Ability.None is not the last element in the enum);
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

        }
    }

    public void Init(ArmAbilityGroup leftArm, ArmAbilityGroup rightArm)
    {
        Debug.Log(init);
        _armReferences[Arm.Left] = new ArmContainer(CreateArmList(leftArm));
        _armReferences[Arm.Right] = new ArmContainer(CreateArmList(rightArm));
        _armReferences[Arm.Left].CurrentAbility = DefaultAbility;
        _armReferences[Arm.Right].CurrentAbility = DefaultAbility;
        ArmAbilityChanged.Invoke(this, new ArmChangeArgs(Arm.Left, DefaultAbility));
         Left arm first ability
        RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate, ReceiveInput.Pressed, execute);
         Right arm first ability
        RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate, ReceiveInput.Pressed, execute);
         Left arm second ability
        RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate2, ReceiveInput.Pressed, execute);
         Right arm second ability
        RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate2, ReceiveInput.Pressed, execute);
         Release events, same order
        RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate, ReceiveInput.Released, cancel);
        RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate, ReceiveInput.Released, cancel);
        RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmActivate2, ReceiveInput.Released, cancel);
        RegisterInput(EA_PlayerInputArguments.EInputType.RightArmActivate2, ReceiveInput.Released, cancel);
         Use ability 2 on keyboard
        RegisterInput(EA_PlayerInputArguments.EInputType.Shift, ReceiveInput.Pressed, shiftOn);
        RegisterInput(EA_PlayerInputArguments.EInputType.Shift, ReceiveInput.Released, shiftOff);
         Ability Switching
        RegisterInput(EA_PlayerInputArguments.EInputType.LeftArmToggle, ReceiveInput.Pressed, nextLeft);
        RegisterInput(EA_PlayerInputArguments.EInputType.RightArmToggle, ReceiveInput.Pressed, nextRight);
    }
    
    private IArmType[] CreateArmList(ArmAbilityGroup armGroup)
    {
        IArmType[] armList = new IArmType[AbilityCount];
        armList[(int) Ability.Reach] = armGroup.reach;
        armList[(int) Ability.Strong] = armGroup.strong;
        armList[(int) Ability.Shock] = armGroup.shock;
        return armList;
    }
    
    public static Arm GetOppositeArm(Arm arm)
    {
        return arm == Arm.Left  Arm.Right  Arm.Left;
    }

     When Ability Used
    void execute(object sender, EA_PlayerInputArguments e)
    {
        switch (e.PlayerInputType)
        {
            case EA_PlayerInputArguments.EInputType.LeftArmActivate
                OnExecuteAbility(Arm.Left, _isShift);
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate
                OnExecuteAbility(Arm.Right, _isShift);
                break;
            case (EA_PlayerInputArguments.EInputType.LeftArmActivate2)
                OnExecuteAbility(Arm.Left, true);
                break;
            case (EA_PlayerInputArguments.EInputType.RightArmActivate2)
                OnExecuteAbility(Arm.Right, true);
                break;
        }
    }

    private void OnExecuteAbility(Arm arm, bool isMovement)
    {
        if(_armReferences[arm].CurrentAbility == Ability.None) return;

        if (isMovement)
        {
            Arm oppositeArm = GetOppositeArm(arm);
            if(_armReferences[oppositeArm].CurrentAbility != Ability.None)
                _armReferences[oppositeArm].GetCurrentArmAction().Deactivate(true);
            _wasAlt[(int)arm] = true;
        }
        _armReferences[arm].GetCurrentArmAction().Deactivate(isMovement);
        _armReferences[arm].GetCurrentArmAction().Activate(isMovement);
        CallArmAbilityTriggered(new AbilityActivationArgs(arm,_armReferences[arm].CurrentAbility, isMovement, true));
    }
    
    void cancel(object sender, EA_PlayerInputArguments e)
    {
        switch (e.PlayerInputType)
        {
            case EA_PlayerInputArguments.EInputType.LeftArmActivate
                OnCancelAbility(Arm.Left, _wasAlt[(int)Arm.Left]);
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate
                OnCancelAbility(Arm.Right, _wasAlt[(int)Arm.Right]);
                break;
            case EA_PlayerInputArguments.EInputType.LeftArmActivate2
                OnCancelAbility(Arm.Left, true);
                break;
            case EA_PlayerInputArguments.EInputType.RightArmActivate2
                OnCancelAbility(Arm.Right, true);
                break;
        }
    }

    private void OnCancelAbility(Arm arm, bool isMovement)
    {
        if (_armReferences[arm].CurrentAbility == Ability.None) return;
        _armReferences[arm].GetCurrentArmAction().Deactivate(isMovement);
        _wasAlt[(int)arm] = false;
        CallArmAbilityTriggered(new AbilityActivationArgs(arm, _armReferences[arm].CurrentAbility, isMovement, false));
    }
    
    private void CallArmAbilityTriggered(AbilityActivationArgs args)
    {
        if (ArmAbilityActivated != null)
            ArmAbilityActivated(this, args);
    }

    void shiftOn(object sender, EA_PlayerInputArguments e) { _isShift = true; }
    void shiftOff(object sender, EA_PlayerInputArguments e) { _isShift = false; }

    void nextLeft(object sender, EA_PlayerInputArguments e) { CycleArmAbility(Arm.Left); }
    void nextRight(object sender, EA_PlayerInputArguments e) { CycleArmAbility(Arm.Right); }

    private void CycleArmAbility(Arm arm)
    {
        Debug.Log(exe);

        _armReferences[arm].StopActions();
        int currentAbilityIndex = (int)_armReferences[arm].CurrentAbility;
        
        if (currentAbilityIndex == AbilityCount - 1)
        {
            _armReferences[arm].CurrentAbility = DefaultAbility;
            CallArmAbilityChanged(arm);
            return;
        }
        if(!isUnlockedAbility(currentAbilityIndex+1))
        {
            _armReferences[arm].CurrentAbility = DefaultAbility;
            CallArmAbilityChanged(arm);
            return;
        }
        _armReferences[arm].CurrentAbility = AllAbilities[currentAbilityIndex + 1];
        CallArmAbilityChanged(arm);
    }
    
    private void CallArmAbilityChanged(Arm arm)
    {
        if(ArmAbilityChanged != null)
        {
            ArmAbilityChanged(this, new ArmChangeArgs(arm, _armReferences[arm].CurrentAbility));
        }
    }

    int convertStringToIndex (String ability) {
        if (ability.ToLower() == reach  ability.ToLower() == growing)
            return (int) Ability.Reach;
        if (ability.ToLower() == strong  ability.ToLower() == strength)
            return (int) Ability.Strong;
        if (ability.ToLower() == shock  ability.ToLower() == electric)
            return (int) Ability.Shock;
        return (int) Ability.None;
    }

    public Ability convertIndexToEnum (int ability) {
        if (ability == 0)
            return Ability.Reach;
        if (ability == 1)
            return Ability.Strong;
        if (ability == 2)
            return Ability.Shock;
        return Ability.None;
    }

    
    public bool isUnlockedAbility (String ability) { return isUnlockedAbility(convertStringToIndex(ability)); }
    public bool isUnlockedAbility (int ability) { return isUnlockedAbility(convertIndexToEnum(ability)); }

    public bool isUnlockedAbility (Ability ability) {
        if(ability == Ability.None)
            return true;
        if(S_GlobalSettings.IsValid() &&  S_GlobalSettings.Instance.GlobalSettingOptions.IsAbilityUnlocked(ability))
            return true;
        return _abilityLock[(int) ability];
    }

    public void setLeftAbility (String ability) {setLeftAbility(convertStringToIndex(ability)); }
    public void setLeftAbility (int ability) {setLeftAbility(convertIndexToEnum(ability)); }
    public void setLeftAbility (Ability ability) {SetArmAbility(Arm.Left, ability); }

    public void setRightAbility (String ability) {setRightAbility(convertStringToIndex(ability)); }
    public void setRightAbility (int ability) {setRightAbility(convertIndexToEnum(ability)); }
    public void setRightAbility (Ability ability) { SetArmAbility(Arm.Right, ability); }
    
    public bool SetArmAbility(Arm arm, Ability ability)
    {
        if (ability == Ability.None) return false;
        if(isUnlockedAbility(ability))
        {
            _armReferences[arm].CurrentAbility = ability;
            CallArmAbilityChanged(arm);
            return true;
        }
        return false;
    }
    
    public Ability GetLeftArmCurrentAbility () { return GetArmCurrentAbility(Arm.Left); }
    public Ability GetRightArmCurrentAbility () { return GetArmCurrentAbility(Arm.Right); }
    public Ability GetArmCurrentAbility(Arm arm) { return _armReferences[arm].CurrentAbility; }

     Takes an ability name string and calls a function to lockunlock that ability
    public bool SetAbilityUnlock (String ability, bool state) { return SetAbilityUnlock(convertStringToIndex(ability), state); }
     Takes an ability enum state and calls a function to lockunlock that ability
    public bool SetAbilityUnlock (int ability, bool state) { return SetAbilityUnlock(convertIndexToEnum(ability), state); }

     locksunlocks the ability at index ind of the AbilityLock array
    public bool SetAbilityUnlock (Ability ability, bool state) 
    {
        if (ability == Ability.None) return false;
        _abilityLock[(int) ability] = state;
        return true;
    }
    
    public void SetAllAbilityUnlocks (bool state) {
        foreach(Ability ability in AllAbilities)
        {
            SetAbilityUnlock(ability, state);
        }
    }

    public void LoadData(ControlSaveFile save)
    {        
        for (int i = 0; i  3; i++)
        {
            _abilityLock[i] = save.armsUnlocked[i];
            SetAbilityUnlock(i, _abilityLock[i]);
        }
        setLeftAbility(save.leftArmAbility);
        setRightAbility(save.rightArmAbility);
        
    }

    public void SaveData(ref ControlSaveFile save)
    {
        for (int i = 0; i  3; i++)
        {
            save.armsUnlocked[i] = _abilityLock[i];
        }
        save.leftArmAbility = GetLeftArmCurrentAbility();
        save.rightArmAbility = GetRightArmCurrentAbility();
    }

}

