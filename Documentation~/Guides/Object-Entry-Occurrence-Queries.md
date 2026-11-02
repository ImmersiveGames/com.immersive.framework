# Object Entry Occurrence Queries

Use this API when a game system already has a logical `ObjectEntryId` and needs
the current physical GameObject associated with it under admitted Route or
Activity content. The query reuses the Framework's owner-aware Runtime Content
binding. It does not create another identity or object registry.

## Select the logical identity in the Inspector

Use `ObjectEntryIdReference` when a serialized field needs only the logical
identity. Read its typed value with `TryGetObjectEntryId`; `ObjectEntryId`
remains the canonical runtime identity type. Unity does not serialize the
existing `readonly ObjectEntryId` directly, so the reference type is only a
serialized transport for its value. The Inspector field accepts a valid ID by
typing or pasting and also offers indexed declarations in a picker.

```csharp
[SerializeField] private ObjectEntryIdReference targetEntry;

if (targetEntry.TryGetObjectEntryId(out ObjectEntryId targetId))
{
    // Pass targetId to the domain API that owns the operation.
}
```

`StableObjectReference` uses the same Inspector field and stores the same
identity. You can type/paste a valid ID or choose one of the picker suggestions.
Its optional **Resolution Owner** is edited separately as an exact `RouteAsset`
or `ActivityAsset`; editing the Object Entry identity never fills or changes
that owner automatically.

The Editor derives candidates from `ObjectEntryDeclaration` components in
Route primary/additional scenes and Activity content-profile scenes, including
scenes that are not currently loaded. It inspects unloaded scenes in Unity
preview scenes and closes those previews after discovery. Prefab assets are
inspected separately; a declaration that exists only in a prefab is labeled
**Prefab template** and does not promise a committed or resolvable physical
occurrence. These candidates are suggestions only: valid IDs outside the index
can still be entered or pasted. No discovery catalog is serialized or used by
runtime resolution.

Picker results include the official ID, GameObject name, hierarchy, scene or
prefab path, and Route/Activity context. Equal names with different IDs remain
separate choices. Repeated declarations with one ID are grouped as that same
logical identity, with their authored contexts shown. The optional owner field
remains the explicit way to select a resolution context.

Empty, invalid, and currently unindexed values remain visible and are flagged;
the picker does not replace them unless the designer explicitly selects a
candidate. Direct entry and paste accept any valid Object Entry ID, including
IDs outside the indexed sources. Discovery covers configured Route/Activity
content and prefab templates. A valid ID may remain unindexed if its declaration
is outside those authoring sources or an asset could not be inspected;
discovery issues are reported in the Framework Editor log. The transient index
is built synchronously when the Inspector field is first drawn and refreshed when the
picker opens; refresh currently inspects all project prefab assets. The cost
in large projects needs practical evaluation.

## Compose the query anchor

Add **Immersive Framework → Object Entry → Occurrence Query** beneath a root
that is included in the Route or Activity content owner's materialized roots.
The Framework binds the anchor only when that owner commits. It unbinds the
anchor on owner rollback or release and on Framework host teardown. A query
anchor's owner determines how long the query capability remains usable; the
reference passed to `Resolve` determines which occurrence is selected.

Keep an explicit reference to the `ObjectEntryOccurrenceQueryBinding` in the
composed gameplay component or pass its `IObjectEntryOccurrenceQuery` interface
to a plain C# service. Do not search globally for an anchor or expose the
Framework host/registry to the consumer.

```csharp
IObjectEntryOccurrenceQuery query = queryBinding;
StableObjectReference reference = StableObjectReference.ForEntry(objectEntryId);
ObjectEntryOccurrenceResolution result = query.Resolve(reference);

if (result.Status == ObjectEntryOccurrenceResolutionStatus.Resolved)
{
    GameObject currentObject = result.Occurrence;
    // Use currentObject for this authorized operation.
}
```

The query does not filter results by the anchor's Route/Activity owner. To
select one exact owner, pass the corresponding typed selector:

```csharp
StableObjectReference.ForRoute(objectEntryId, routeAsset);
StableObjectReference.ForActivity(objectEntryId, activityAsset);
```

Those selectors preserve exact authored definition identity. Their stable IDs
alone do not grant owner or release authority.

## Resolution outcomes

| Status | Meaning |
|---|---|
| `Resolved` | Exactly one live, committed occurrence matched. `Occurrence` is the current GameObject at the time of the call. |
| `Unavailable` | The query anchor is unbound, no live committed occurrence matches, or the bound Unity object has been destroyed. |
| `InvalidReference` | The reference has no valid `ObjectEntryId` or its optional typed Route/Activity selector is invalid. |
| `Ambiguous` | More than one live committed occurrence matches. The Framework returns no candidate. Add an exact owner selector when that reflects the consumer's intent. |

An invalid reference returns `InvalidReference` even while its query anchor is
unbound. A valid reference queried before owner commit or after the query
anchor's owner is released returns `Unavailable`.

## Read-only and authorization boundary

`Resolve` reads committed bindings only. It never instantiates or registers
content, changes the binding table, modifies a GameObject or selects an
arbitrary candidate. A destroyed GameObject is ignored without query-time
cleanup; its Route/Activity owner remains responsible for releasing its
binding records.

The returned GameObject is point-in-time physical evidence, not a persistent
identity. Query again when the consumer needs current availability; do not
serialize or retain a result as the identity of the logical entry. The query
does not assert `activeInHierarchy` and does not imply gameplay readiness,
perception, knowledge, targeting or authorization to mutate the object. Those
policies belong to the consumer.

## Manual validation

After package import and compile, validate in a Unity consumer project:

1. Add fields of type `ObjectEntryIdReference` and `StableObjectReference` to
   an authoring component. Close the declaration's configured scene, select the
   same logical Object Entry in both fields, and save/reopen the asset. Confirm
   the typed ID is unchanged and selecting the identity did not populate or
   alter `StableObjectReference`'s optional owner. Enter and paste a valid ID
   not present in the picker and confirm both remain valid.
2. Add same-named declarations with different IDs in separate configured
   contexts. Confirm the picker distinguishes them by ID and source. Select a
   declaration that exists only in a prefab and confirm it is labeled as a
   template, without implying a physical occurrence.
3. Put an anchor in Route-owned content and an Object Entry in Activity-owned
   content. Confirm a valid query is unavailable before Activity commit and
   resolves the declared GameObject after commit.
4. Admit another occurrence with the same `ObjectEntryId`. Confirm an unscoped
   reference reports `Ambiguous` and exact Route/Activity selectors resolve
   only their matching owner.
5. Release the Activity while retaining the Route-owned query interface.
   Confirm the target becomes `Unavailable` and the interface remains usable.
6. Roll back a Route/Activity admission and tear down the Framework host while
   retaining a query interface. Confirm each returns `Unavailable`.
7. Destroy an admitted target while its owner remains committed. Confirm the
   query reports `Unavailable` and does not modify the binding table.
8. Confirm existing Reset `StableObjectReference` targeting still resolves
   Resettable/ResetComposition through its existing consumer path.

The query API and metadata are Experimental. The Project Owner reports the
QAFramework EditMode suite passed 491/491 with zero failures in Unity
`6000.5.0f1`, including the Object Entry host-teardown regression. No raw NUnit
XML or execution SHA was supplied. This package test evidence does not certify
consumer gameplay authorization, PEGA integration or consumer Play Mode
behavior.
