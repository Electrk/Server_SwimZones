datablock TriggerData ( SwimZoneTrigger )
{
	tickPeriodMS = 150;
};

function SwimZoneTrigger::updateObjectSwimZone ( %data, %trigger, %object )
{
	%swimZone = %object.swimZone;

	if ( isObject (%swimZone) && %object.lastSwimTriggerType $= "" )
	{
		%type = %trigger.swimZoneTriggerType;

		// Prevents jittering back and forth when players are standing in two triggers at once.
		%object.lastSwimTriggerType = %type;

		if ( %type == $SwimZones::TriggerTypeEnter )
		{
			SwimZones.setSwimZoneEnabled (%swimZone, true);
		}
		else if ( %type == $SwimZones::TriggerTypeLeave )
		{
			SwimZones.setSwimZoneEnabled (%swimZone, false);
		}
	}
}

function SwimZoneTrigger::onEnterTrigger ( %data, %trigger, %object )
{
	%data.updateObjectSwimZone (%trigger, %object);
}

function SwimZoneTrigger::onTickTrigger ( %data, %trigger, %object )
{
	%data.updateObjectSwimZone (%trigger, %object);
}

function SwimZoneTrigger::onLeaveTrigger ( %data, %trigger, %object )
{
	if ( %object.lastSwimTriggerType == %trigger.swimZoneTriggerType )
	{
		%object.lastSwimTriggerType = "";
	}
}
