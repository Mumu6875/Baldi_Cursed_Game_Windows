# Unity 6.3 LTS migration status

Target Editor: `6000.3.22f1` (changeset `1c726e1fb402`).

Prepared changes:

- Project editor version moved from Unity 2019.4 to Unity 6.3 LTS.
- Legacy Analytics and Package Manager UI dependencies removed.
- Legacy standalone TextMesh Pro dependency replaced by Unity 6's
  `com.unity.ugui` 2.0.0 package, which includes TextMesh Pro.
- Removed obsolete built-in Umbra, legacy Analytics, and VR module entries.
- Android minimum API raised from 23 to 26 for Unity 6 compatibility.
- Application version raised to 1.2.0 and Android version code to 3.

Validation boundary:

The source manifest and project settings were validated statically. A Unity
Editor is required to rebuild the Asset Database, reserialize scenes and
prefabs, compile against Unity 6 APIs, and produce the final Android player.
Use a clean Unity Build Automation run and diagnose the first real Editor error
if the initial migration import does not complete.
