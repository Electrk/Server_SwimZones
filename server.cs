function defaultValue ( %value, %default )
{
	return (%value $= "" ? %default : %value);
}

function SelectiveSwimming_init ()
{
	if ( isObject (SelectiveSwimmingSO) )
	{
		SelectiveSwimmingSO.delete ();
	}

	MissionCleanup.add (new ScriptObject (SelectiveSwimmingSO)
	{
		class = SelectiveSwimming;
	});

	SelectiveSwimmingSO.loop ();
	SelectiveSwimmingSO.initPrefs ();
}

function SelectiveSwimming::onAdd ( %this )
{
	%this.swimZones = new SimSet ();
}

function SelectiveSwimming::onRemove ( %this )
{
	%this.swimZones.deleteAll ();
	%this.swimZones.delete ();
}

function SelectiveSwimming::initPrefs ()
{
	//* Constants *//

	// Various mod values.
	$SelectiveSwimming::LoopTick = 33;

	// Properties of the players' swim zones.
	$SelectiveSwimming::WaterViscosity = 40;
	$SelectiveSwimming::WaterDensity = 0.7;
	$SelectiveSwimming::WaterGravityMod = 0;

	// How to scale the swim zone according to the player's bounding box.
	$SelectiveSwimming::WaterScaleMultX = 0.5;
	$SelectiveSwimming::WaterScaleMultY = 0.5;
	$SelectiveSwimming::WaterScaleMultZ = 0.4;

	// We don't want to pollute the $TypeMasks::* variable space.
	$SelectiveSwimming::TypeMask = $TypeMasks::CorpseObjectType
		| $TypeMasks::ItemObjectType
		| $TypeMasks::PlayerObjectType
		| $TypeMasks::VehicleObjectType;

	//* Preferences *//

	$Pref::Server::SelSwim::SurfaceHeight = defaultValue ($Pref::Server::SelSwim::SurfaceHeight, 30);
}

function SelectiveSwimming::loop ( %this )
{
	cancel (%this.mainLoop);

	%swimZones = %this.swimZones;
	%count = %swimZones.getCount ();

	for ( %i = 0; %i < %count; %i++ )
	{
		%swimZone = %swimZones.getObject (%i);

		if ( isObject (%swimZone.selSwimObj) )
		{
			%this.moveSwimZone (%swimZone);
		}
	}

	%this.mainLoop = %this.schedule ($SelectiveSwimming::LoopTick, "loop");
}

// Moves a swim zone to its player.  Assumes `%swimZone.selSwimObj` exists and is a SceneObject.
function SelectiveSwimming::moveSwimZone ( %this, %swimZone )
{
	%scale = %swimZone.getScale ();
	%scaleX = getWord (%scale, 0);
	%scaleY = getWord (%scale, 1);
	%scaleZ = getWord (%scale, 2);

	%position = %swimZone.selSwimObj.position;

	// Some adjustments are needed to center the swim zone.
	%newPosX = getWord (%position, 0) - (%scaleX / 2);
	%newPosY = getWord (%position, 1) + (%scaleY / 2);

	// We fudge this Z coordinate a bit with the `* 0.1` to prevent the bottom of the swim zone from
	// being flush with the ground, which creates this weird half-walking effect when the player is
	// touching the ground.
	%newPosZ = getWord (%position, 2) - (%scaleZ * 0.1);

	// Clamp the swim zone's Z position so that the top of it doesn't go above the surface height.
	%newPosZ = mClampF (%newPosZ, -1, $Pref::Server::SelSwim::SurfaceHeight - %scaleZ);

	%swimZone.setTransform (%newPosX SPC %newPosY SPC %newPosZ);
}

// Creates a swim zone and attaches it to an object.  Returns 0 if it cannot attach the swim zone to
// the specified object.
function SelectiveSwimming::createSwimZone ( %this, %object )
{
	%swimZone = 0;

	if ( %this.canAttachSwimZone (%object) )
	{
		%swimZone = new PhysicalZone ()
		{
			isWater = true;
			waterViscosity = $SelectiveSwimming::WaterViscosity;
			waterDensity = $SelectiveSwimming::WaterDensity;
			gravityMod = $SelectiveSwimming::WaterGravityMod;
			polyhedron = "0 0 0 1 0 0 0 -1 0 0 0 1";
		};

		MissionCleanup.add (%swimZone);
		%this.swimZones.add (%swimZone);

		%this.attachSwimZone (%swimZone, %object);
	}

	return %swimZone;
}

// Checks whether a swim zone can attach to an object.
function SelectiveSwimming::canAttachSwimZone ( %this, %object )
{
	return %object.canAttachSwimZone
		&& !isObject (%object.selSwimZone)
		&& !isObject (%swimZone.selSwimObj)
		&& (%object.getType () & $SelectiveSwimming::TypeMask);
}

// Attaches a swim zone to an object, provided that it's not attached already.
function SelectiveSwimming::attachSwimZone ( %this, %swimZone, %object )
{
	if ( %this.canAttachSwimZone (%object) )
	{
		%object.selSwimZone = %swimZone;
		%swimZone.selSwimObj = %object;

		%this.updateSwimZoneScale (%swimZone);
		%this.moveSwimZone (%swimZone);
	}
}

// Detaches a swim zone from an object, provided that it's actually attached to an object.
function SelectiveSwimming::detachSwimZone ( %this, %swimZone )
{
	%object = %swimZone.selSwimObj;
	%objectZone = %object.selSwimZone;

	%swimZone.selSwimObj = "";

	// Make sure this swim zone is attached to the correct object.
	if ( isObject (%objectZone) && %objectZone.getID () == %swimZone.getID () )
	{
		%object.selSwimZone = "";
	}
}

// Deletes a swim zone and detaches it from its object.
function SelectiveSwimming::deleteSwimZone ( %this, %swimZone )
{
	%this.detachSwimZone (%swimZone);
	%swimZone.delete ();
}

// Updates a swim zone's scale based on its player's bounding box and scale.
// Assumes `%swimZone.selSwimObj` exists.
function SelectiveSwimming::updateSwimZoneScale ( %this, %swimZone )
{
	%object = %swimZone.selSwimObj;

	if ( %object.getType () & $TypeMasks::PlayerObjectType )
	{
		%bounds = %object.dataBlock.boundingBox;
	}
	else
	{
		%box = %object.getObjectBox ();
		%bounds = vectorSub (getWords (%box, 3, 5), getWords (%box, 0, 2));
	}

	%boundsX = getWord (%bounds, 0);
	%boundsY = getWord (%bounds, 1);
	%boundsZ = getWord (%bounds, 2);

	%scale = %object.getScale ();
	%scaleX = getWord (%scale, 0);
	%scaleY = getWord (%scale, 1);
	%scaleZ = getWord (%scale, 2);

	%swimZone.setScale ((%boundsX * $SelectiveSwimming::WaterScaleMultX * %scaleX)
		SPC (%boundsY * $SelectiveSwimming::WaterScaleMultY * %scaleY)
		SPC (%boundsZ * $SelectiveSwimming::WaterScaleMultZ * %scaleZ));
}

// Enables/disables the swim zone.
function SelectiveSwimming::setSwimZoneEnabled ( %this, %swimZone, %enabled )
{
	%swimZone.isWater = %enabled;
	%swimZone.gravityMod = !%enabled;
	%swimZone.sendUpdate ();
}

exec ("./package.cs");
