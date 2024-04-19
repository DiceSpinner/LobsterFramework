# Ability System
This article will serve as an manual to how the Ability System functions in LobsterFramework.

# Intro
Before we begin, it is necessary to clarify what kind of ability system we're dealing with here. The idea is directly taken from League of Legends, a MOBA game of 10 where each player play as a champion with a couple unique abilities. Here're some observations.

1. Most abilities have cooldowns which represents the amount of time it'll take for them to be available to cast again. 
2. Some abilities can be interacted with after the initial cast.
3. Each ability's effect is somewhat unique, but the extent of these effects (i.e. how much health is restored on a healing spell) is governed by a set of champion stats (Attack Damage, Ability Power, etc) in that is shared by all champions. 
4. Some abilities feature effects that is affected by stats/resources that only exists on certain champions. 
5. Abilities are ranked by their priorities, abilities with higher priorities are executed earlier than others. 
6. Abilities can be interrupted

There're some other features I will not go in detail here. Overall, the design goal is for it to have the following properties:
- Ability can have unique attributes
- Ability can have unfixed duration at runtime
- Data can be shared between abilities
- Ability must be evaluated in a certain order to make results deterministic
- Ability can be interrupted
- Ability can be react to events
- Ability can be communicated with during its execution
- Configurable ability settings, the configuration can be saved assets and should act like other assets and be used by any entity.
- Editor Support: Having custom inspector that allows developers to easily configure ability settings 
- Code-Backed & Single Threaded. All abilities and the associated parts should be implemented with code. Although developers can edit abilities settings in the inspector, this system is not intended for creation of abilities inside editor.

# Core Classes
It is important to understand these classes before diving into the usages.
## AbilityManager
Attach this component to the character to enable it to cast abilities. This component takes in an [AbilityData](#abilitydata) as input. Calls to enqueue, query, terminate, send event to and communication with abilities should only be done during the *Update* event.

## AbilityExecutor
A singleton component to be attached to a persistent manager object. This component handles ability execution during *LateUpdate* event.

## AbilityData
An asset object that defines a set of [Abilities](#ability) and [Ability Components](#abilitycomponent). Can be edited using inspector.

## AbilityComponent
An asset object that defines a resource shared by all abilities.

## Ability
An asset object that defines an ability in the Ability System. To create new abilities, you must subclass it and implement the required methods. It comes with 3 complimentary classes that you must define: [AbilityConfig](#abilityconfig), [AbilityChannel](#abilitychannel), [AbilityContext](#abilitycontext).

## AbilityConfig
An asset object that defines the setting of the ability. A new ability needs to define **{#NameOfAbility}Config** that inherit from this class or its parent's config class if there's one.

## AbilityChannel
Allows client code to communicate with the ability when it is being runned. A new ability needs to define **{#NameOfAbility}Channel** that inherit from this class or its parent's channel class if there's one. You should not define constructors with parameters for this class. For custom initialization see **Ability.InitializeContext()**.

## AbilityContext
Stores context variables use by the ability during its execution. A new ability needs to define **{#NameOfAbility}Context** that inherit from this class or its parent's context class if there's one. You should not define constructors with parameters for this class. For custom initialization see **Ability.InitializeContext()**.
