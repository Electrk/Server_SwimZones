exec ("./init.cs");
exec ("./trigger.cs");
exec ("./packages.cs");

// ------------------------------------------------


function SwimZones::onAdd ( %this )
{
	%this.swimZones = new SimSet ();
}

function SwimZones::onRemove ( %this )
{
	%this.deleteAllSwimZones ();
	%this.swimZones.delete ();
}

// Main update loop
function SwimZones::loop ( %this )
{
	cancel (%this.mainLoop);

	%swimZones = %this.swimZones;
	%count = %swimZones.getCount ();

	for ( %i = 0; %i < %count; %i++ )
	{
		%swimZone = %swimZones.getObject (%i);

		if ( isObject (%swimZone.swimZoneObj) )
		{
			%this.moveSwimZone (%swimZone);
		}
	}

	%this.mainLoop = %this.schedule ($SwimZones::LoopTick, "loop");
}

// Moves a swim zone to its object.
//
// Assumes `%swimZone.swimZoneObj` exists and is a SceneObject.
function SwimZones::moveSwimZone ( %this, %swimZone )
{
	%scale = %swimZone.getScale ();
	%scaleX = getWord (%scale, 0);
	%scaleY = getWord (%scale, 1);
	%scaleZ = getWord (%scale, 2);

	%position = %swimZone.swimZoneObj.position;

	// Some adjustments are needed to center the swim zone.
	%newPosX = getWord (%position, 0) - (%scaleX / 2);
	%newPosY = getWord (%position, 1) + (%scaleY / 2);

	// We fudge this Z coordinate a bit with the `* 0.1` to prevent the bottom of the swim zone from
	// being flush with the ground, which creates this weird half-walking effect when the player is
	// touching the ground.
	%newPosZ = getWord (%position, 2) - (%scaleZ * 0.1);

	// Clamp the swim zone's Z position so that the top of it doesn't go above the surface height.
	%newPosZ = mClampF (%newPosZ, -1, $Pref::Server::SwimZones::SurfaceHeight - %scaleZ);

	%swimZone.setTransform (%newPosX SPC %newPosY SPC %newPosZ);
}

// Creates a swim zone and attaches it to an object.
//
// Returns 0 if it cannot attach the swim zone to the specified object.
function SwimZones::createSwimZone ( %this, %object )
{
	%swimZone = 0;

	if ( %this.canAttachSwimZone (%object) )
	{
		%swimZone = new PhysicalZone ()
		{
			isWater = true;
			waterViscosity = $SwimZones::WaterViscosity;
			waterDensity = $SwimZones::WaterDensity;
			gravityMod = $SwimZones::WaterGravityMod;
			polyhedron = "0 0 0 1 0 0 0 -1 0 0 0 1";
		};

		MissionCleanup.add (%swimZone);
		%this.swimZones.add (%swimZone);

		%this.attachSwimZone (%swimZone, %object);
	}

	return %swimZone;
}

// Returns whether a new or existing swim zone can be attached to an object.
//
// If a swim zone is passed to `%swimZone`, the function checks if it can be attached to the object.
// Otherwise, it just checks if a new one can.
function SwimZones::canAttachSwimZone ( %this, %object, %swimZone )
{
	%canAttach = %object.canAttachSwimZone
		&& !isObject (%object.swimZone)
		&& (%object.getType () & $SwimZones::TypeMask);

	if ( %swimZone !$= "" )
	{
		// Make sure the swim zone isn't already attached to another object.
		%canAttach = %canAttach && !isObject (%swimZone.swimZoneObj);
	}

	return %canAttach;
}

// Attaches a swim zone to an object, provided that it's not attached to something already.
function SwimZones::attachSwimZone ( %this, %swimZone, %object )
{
	if ( %this.canAttachSwimZone (%object, %swimZone) )
	{
		%object.swimZone = %swimZone;
		%swimZone.swimZoneObj = %object;

		%this.updateSwimZoneScale (%swimZone);
		%this.moveSwimZone (%swimZone);
	}
}

// Detaches a swim zone from an object, provided that it's actually attached to an object.
function SwimZones::detachSwimZone ( %this, %swimZone )
{
	%object = %swimZone.swimZoneObj;
	%objectZone = %object.swimZone;

	%swimZone.swimZoneObj = "";

	// Make sure this swim zone is attached to the correct object.
	if ( isObject (%objectZone) && %objectZone.getID () == %swimZone.getID () )
	{
		%object.swimZone = "";
	}
}

// Deletes a swim zone and detaches it from its object.
function SwimZones::deleteSwimZone ( %this, %swimZone )
{
	%this.detachSwimZone (%swimZone);
	%swimZone.delete ();
}

// Deletes all the swim zones on the map.
function SwimZones::deleteAllSwimZones ( %this )
{
	%swimZones = %this.swimZones;

	while ( %swimZones.getCount () )
	{
		%this.deleteSwimZone (%swimZones.getObject (0));
	}
}

// Updates a swim zone's scale based on its object.
//
// If it's attached to a player, it scales based on the datablock's bounding box as well as the
// player's scale.
// Otherwise, it scales based on the object's world box.
//
// Assumes `%swimZone.swimZoneObj` exists.
function SwimZones::updateSwimZoneScale ( %this, %swimZone )
{
	%object = %swimZone.swimZoneObj;

	if ( %object.getType () & $TypeMasks::PlayerObjectType )
	{
		//* Special handling for players because the player's world box is MASSIVE *//

		%bounds = %object.dataBlock.boundingBox;

		%scale = %object.getScale ();
		%scaleX = getWord (%scale, 0);
		%scaleY = getWord (%scale, 1);
		%scaleZ = getWord (%scale, 2);

		%newScale = getWord (%bounds, 0) * %scaleX * $SwimZones::PlayerScaleMultX
			SPC getWord (%bounds, 1) * %scaleY * $SwimZones::PlayerScaleMultY
			SPC getWord (%bounds, 2) * %scaleZ * $SwimZones::PlayerScaleMultZ;
	}
	else
	{
		%box = %object.getWorldBox ();
		%bounds = vectorSub (getWords (%box, 3, 5), getWords (%box, 0, 2));

		%newScale = getWord (%bounds, 0) * $SwimZones::ObjectScaleMultX
			SPC getWord (%bounds, 1) * $SwimZones::ObjectScaleMultY
			SPC getWord (%bounds, 2) * $SwimZones::ObjectScaleMultZ;
	}

	%swimZone.setScale (%newScale);
}

// Enables/disables the swim zone.
function SwimZones::setSwimZoneEnabled ( %this, %swimZone, %enabled )
{
	if ( %enabled )
	{
		%swimZone.activate ();
	}
	else
	{
		%swimZone.deactivate ();
	}
}
