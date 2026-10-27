import { useEffect, useState } from 'react'
import { Check, Loader2, Plus } from 'lucide-react'
import { useAddLookupMutation, useGetLookupsQuery } from '@/services/apiSlice'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

// A drop-down with a fixed set of values (seeded from the strategy) that also lets the user add a
// new value, which is saved so future jobs pick from the same list.
export function LookupField({
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
  const [text, setText] = useState(value ?? '')

  useEffect(() => {
    setText(value ?? '')
  }, [value])

  const listId = `lookup-${category}`
  const trimmed = text.trim()
  const exists = options.some((option) => option.value.toLowerCase() === trimmed.toLowerCase())

  const add = async () => {
    if (!trimmed) return
    try {
      const created = await addLookup({ category, value: trimmed }).unwrap()
      setText(created.value)
      onChange(created.value)
    } catch {
      onChange(trimmed)
    }
  }

  return (
    <div className="flex gap-2">
      <Input
        list={listId}
        value={text}
        onChange={(e) => {
          setText(e.target.value)
          onChange(e.target.value)
        }}
        placeholder={placeholder}
      />
      <datalist id={listId}>
        {options.map((option) => (
          <option key={option.id} value={option.value} />
        ))}
      </datalist>
      <Button
        type="button"
        variant="outline"
        onClick={add}
        disabled={!trimmed || exists || adding}
        title={exists ? 'Already in the list' : 'Save this value to the list for future use'}
      >
        {adding ? <Loader2 className="animate-spin" /> : exists ? <Check /> : <Plus />}
        Add
      </Button>
    </div>
  )
}