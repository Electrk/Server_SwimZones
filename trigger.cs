datablock TriggerData ( SelSwimTrigger )
{
	tickPeriodMS = 150;
};

function SelSwimTrigger::updateObjectSwimZone ( %data, %trigger, %object )
{
	%swimZone = %object.selSwimZone;

	if ( isObject (%swimZone) && %object.lastSwimTriggerType $= "" )
	{
		%type = %trigger.selSwimTriggerType;

		// Prevents jittering back and forth when players are standing in two triggers at once.
		%object.lastSwimTriggerType = %type;

		if ( %type == $SelectiveSwimming::TriggerTypeEnter )
		{
			SelectiveSwimming.setSwimZoneEnabled (%swimZone, true);
		}
		else if ( %type == $SelectiveSwimming::TriggerTypeLeave )
		{
			SelectiveSwimming.setSwimZoneEnabled (%swimZone, false);
		}
	}
}

function SelSwimTrigger::onEnterTrigger ( %data, %trigger, %object )
{
	%data.updateObjectSwimZone (%trigger, %object);
}

function SelSwimTrigger::onTickTrigger ( %data, %trigger, %object )
{
	%data.updateObjectSwimZone (%trigger, %object);
}

function SelSwimTrigger::onLeaveTrigger ( %data, %trigger, %object )
{
	if ( %object.lastSwimTriggerType == %trigger.selSwimTriggerType )
	{
		%object.lastSwimTriggerType = "";
	}
}
