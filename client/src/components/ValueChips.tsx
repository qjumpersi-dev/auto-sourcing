// Shows how a comma-separated input is split into individual search values, so it is clear the
// search matches ANY of them rather than the whole string.
export function ValueChips({ value, verb = 'Searches for' }: { value: string; verb?: string }) {
  const items = (value ?? '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)

  if (items.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap items-center gap-1 pt-1">
      <span className="text-xs text-muted-foreground">
        {items.length === 1 ? `${verb}:` : `${verb} any of ${items.length}:`}
      </span>
      {items.map((item) => (
        <span
          key={item}
          className="rounded-full border bg-muted/40 px-2 py-0.5 text-xs"
        >
          {item}
        </span>
      ))}
    </div>
  )
}