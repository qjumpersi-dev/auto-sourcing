import { useState } from 'react'
import { Loader2, Plus, X } from 'lucide-react'
import { useAddLookupMutation, useGetLookupsQuery } from '@/services/apiSlice'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

// A multi-value pick-list: choose from the saved list or add a new value, which is saved for
// future use. Stored as a comma-separated string on the job.
export function LookupMultiField({
  category,
  value,
  onChange,
  placeholder,
}: {
  category: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
}) {
  const { data: options = [] } = useGetLookupsQuery(category)
  const [addLookup, { isLoading: adding }] = useAddLookupMutation()
  const [text, setText] = useState('')

  const items = value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)

  const add = async () => {
    const next = text.trim()
    if (!next) return

    if (!options.some((option) => option.value.toLowerCase() === next.toLowerCase())) {
      try {
        await addLookup({ category, value: next }).unwrap()
      } catch {
        // keep the value on this job even if saving to the shared list fails
      }
    }

    if (!items.some((item) => item.toLowerCase() === next.toLowerCase())) {
      onChange([...items, next].join(', '))
    }
    setText('')
  }

  const remove = (item: string) => {
    onChange(items.filter((entry) => entry !== item).join(', '))
  }

  const listId = `lookup-multi-${category}`

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-1.5">
        {items.length === 0 ? (
          <span className="text-xs text-muted-foreground">Nothing added yet.</span>
        ) : (
          items.map((item) => (
            <span
              key={item}
              className="inline-flex items-center gap-1 rounded-full border bg-muted/40 px-2 py-0.5 text-xs"
            >
              {item}
              <button
                type="button"
                onClick={() => remove(item)}
                aria-label={`Remove ${item}`}
                className="text-muted-foreground hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          ))
        )}
      </div>
      <div className="flex gap-2">
        <Input
          list={listId}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              add()
            }
          }}
          placeholder={placeholder}
        />
        <datalist id={listId}>
          {options.map((option) => (
            <option key={option.id} value={option.value} />
          ))}
        </datalist>
        <Button type="button" variant="outline" onClick={add} disabled={!text.trim() || adding}>
          {adding ? <Loader2 className="animate-spin" /> : <Plus />}
          Add
        </Button>
      </div>
    </div>
  )
}