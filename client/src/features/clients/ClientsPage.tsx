import { useState, type FormEvent } from 'react'
import { Building2, Loader2, Pencil, Save, Trash2, UserPlus } from 'lucide-react'
import {
  useCreateClientMutation,
  useDeleteClientMutation,
  useGetClientsQuery,
  useUpdateClientMutation,
} from '@/services/apiSlice'
import { formatDate } from '@/lib/formatDate'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'

interface ClientDraft {
  name: string
  contactName: string
  contactEmail: string
  contactPhone: string
  notes: string
}

const emptyDraft: ClientDraft = {
  name: '',
  contactName: '',
  contactEmail: '',
  contactPhone: '',
  notes: '',
}

export function ClientsPage() {
  const { data: clients = [], isLoading } = useGetClientsQuery()
  const [createClient, { isLoading: creating }] = useCreateClientMutation()
  const [updateClient, { isLoading: updating }] = useUpdateClientMutation()
  const [deleteClient] = useDeleteClientMutation()

  const [draft, setDraft] = useState<ClientDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState<string | null>(null)

  const updateField = (key: keyof ClientDraft, value: string) => {
    setDraft((current) => ({ ...current, [key]: value }))
    setSaved(null)
  }

  const reset = () => {
    setDraft(emptyDraft)
    setEditingId(null)
    setError(null)
  }

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    setSaved(null)

    if (!draft.name.trim()) {
      setError('Client name is required.')
      return
    }

    const payload = {
      name: draft.name.trim(),
      contactName: draft.contactName || null,
      contactEmail: draft.contactEmail || null,
      contactPhone: draft.contactPhone || null,
      notes: draft.notes || null,
    }

    try {
      if (editingId !== null) {
        await updateClient({ id: editingId, ...payload }).unwrap()
        setSaved(`Updated ${payload.name}.`)
      } else {
        await createClient(payload).unwrap()
        setSaved(`Created ${payload.name}.`)
      }
      reset()
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not save the client.')
    }
  }

  const onEdit = (client: (typeof clients)[number]) => {
    setEditingId(client.id)
    setDraft({
      name: client.name,
      contactName: client.contactName ?? '',
      contactEmail: client.contactEmail ?? '',
      contactPhone: client.contactPhone ?? '',
      notes: client.notes ?? '',
    })
    setSaved(null)
    setError(null)
  }

  const onDelete = async (id: number, name: string) => {
    if (!window.confirm(`Delete ${name}? Jobs assigned to this client will become unassigned.`)) {
      return
    }
    try {
      await deleteClient(id).unwrap()
      if (editingId === id) {
        reset()
      }
    } catch {
      setError('Could not delete the client.')
    }
  }

  const saving = creating || updating

  return (
    <div className="space-y-6">
      <div>
        <h2 className="flex items-center gap-2 text-xl font-semibold">
          <Building2 className="h-5 w-5 text-blue-600" />
          Clients
        </h2>
        <p className="text-sm text-muted-foreground">
          The hiring organisations you recruit for. Jobs are grouped by client so you can filter and
          report across all their roles.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{editingId !== null ? 'Edit client' : 'Add a client'}</CardTitle>
          <CardDescription>A client is the employer a job is for.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 sm:grid-cols-2" onSubmit={onSubmit}>
            <div className="space-y-1.5">
              <Label>Client name *</Label>
              <Input
                value={draft.name}
                onChange={(e) => updateField('name', e.target.value)}
                placeholder="e.g. Acme Data Centres"
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label>Contact name</Label>
              <Input
                value={draft.contactName}
                onChange={(e) => updateField('contactName', e.target.value)}
                placeholder="e.g. Jane Smith"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Contact email</Label>
              <Input
                type="email"
                value={draft.contactEmail}
                onChange={(e) => updateField('contactEmail', e.target.value)}
                placeholder="jane@acme.com"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Contact phone</Label>
              <Input
                value={draft.contactPhone}
                onChange={(e) => updateField('contactPhone', e.target.value)}
                placeholder="+64 21 123 4567"
              />
            </div>
            <div className="space-y-1.5 sm:col-span-2">
              <Label>Notes</Label>
              <Textarea
                value={draft.notes}
                onChange={(e) => updateField('notes', e.target.value)}
                placeholder="Anything useful about this client..."
                rows={2}
              />
            </div>

            <div className="sm:col-span-2">
              {error && <p className="text-sm text-destructive">{error}</p>}
              {saved && <p className="text-sm text-emerald-600">{saved}</p>}
            </div>

            <div className="flex gap-2 sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? <Loader2 className="animate-spin" /> : editingId !== null ? <Save /> : <UserPlus />}
                {editingId !== null ? 'Save changes' : 'Add client'}
              </Button>
              {editingId !== null && (
                <Button type="button" variant="outline" onClick={reset}>
                  Cancel
                </Button>
              )}
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>All clients ({clients.length})</CardTitle>
          <CardDescription>Every hiring organisation in this workspace.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading...
            </p>
          ) : clients.length === 0 ? (
            <p className="text-sm text-muted-foreground">No clients yet. Add one above.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Contact</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Phone</TableHead>
                  <TableHead>Added</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {clients.map((client) => (
                  <TableRow key={client.id}>
                    <TableCell className="font-medium">{client.name}</TableCell>
                    <TableCell>{client.contactName || '—'}</TableCell>
                    <TableCell>{client.contactEmail || '—'}</TableCell>
                    <TableCell>{client.contactPhone || '—'}</TableCell>
                    <TableCell>{formatDate(client.createdAt)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-1">
                        <Button size="sm" variant="outline" onClick={() => onEdit(client)}>
                          <Pencil />
                          Edit
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => onDelete(client.id, client.name)}>
                          <Trash2 />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}