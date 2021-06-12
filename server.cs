exec ("./init.cs");
exec ("./trigger.cs");
exec ("./packages.cs");

// ------------------------------------------------


function SelectiveSwimming::onAdd ( %this )
{
	%this.swimZones = new SimSet ();
}

function SelectiveSwimming::onRemove ( %this )
{
	%this.deleteAllSwimZones ();
	%this.swimZones.delete ();
}

// Main update loop
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

// Moves a swim zone to its object.
//
// Assumes `%swimZone.selSwimObj` exists and is a SceneObject.
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

// Creates a swim zone and attaches it to an object.
//
// Returns 0 if it cannot attach the swim zone to the specified object.
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

// Returns whether a new or existing swim zone can be attached to an object.
//
// If a swim zone is passed to `%swimZone`, the function checks if it can be attached to the object.
// Otherwise, it just checks if a new one can.
function SelectiveSwimming::canAttachSwimZone ( %this, %object, %swimZone )
{
	%canAttach = %object.canAttachSwimZone
		&& !isObject (%object.selSwimZone)
		&& (%object.getType () & $SelectiveSwimming::TypeMask);

	if ( %swimZone !$= "" )
	{
		// Make sure the swim zone isn't already attached to another object.
		%canAttach = %canAttach && !isObject (%swimZone.selSwimObj);
	}

	return %canAttach;
}

// Attaches a swim zone to an object, provided that it's not attached to something already.
function SelectiveSwimming::attachSwimZone ( %this, %swimZone, %object )
{
	if ( %this.canAttachSwimZone (%object, %swimZone) )
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

// Deletes all the swim zones on the map.
function SelectiveSwimming::deleteAllSwimZones ( %this )
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
// Assumes `%swimZone.selSwimObj` exists.
function SelectiveSwimming::updateSwimZoneScale ( %this, %swimZone )
{
	%object = %swimZone.selSwimObj;

	if ( %object.getType () & $TypeMasks::PlayerObjectType )
	{
		//* Special handling for players because the player's world box is MASSIVE *//

		%bounds = %object.dataBlock.boundingBox;

		%scale = %object.getScale ();
		%scaleX = getWord (%scale, 0);
		%scaleY = getWord (%scale, 1);
		%scaleZ = getWord (%scale, 2);

		%newScale = getWord (%bounds, 0) * %scaleX * $SelectiveSwimming::PlayerScaleMultX
			SPC getWord (%bounds, 1) * %scaleY * $SelectiveSwimming::PlayerScaleMultY
			SPC getWord (%bounds, 2) * %scaleZ * $SelectiveSwimming::PlayerScaleMultZ;
	}
	else
	{
		%box = %object.getWorldBox ();
		%bounds = vectorSub (getWords (%box, 3, 5), getWords (%box, 0, 2));

		%newScale = getWord (%bounds, 0) * $SelectiveSwimming::ObjectScaleMultX
			SPC getWord (%bounds, 1) * $SelectiveSwimming::ObjectScaleMultY
			SPC getWord (%bounds, 2) * $SelectiveSwimming::ObjectScaleMultZ;
	}

	%swimZone.setScale (%newScale);
}

// Enables/disables the swim zone.
function SelectiveSwimming::setSwimZoneEnabled ( %this, %swimZone, %enabled )
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
