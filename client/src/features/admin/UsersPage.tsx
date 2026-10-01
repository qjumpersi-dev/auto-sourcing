import { useState, type FormEvent } from 'react'
import { Loader2, UserPlus } from 'lucide-react'
import { useCreateUserMutation, useGetUsersQuery } from '@/services/apiSlice'
import { formatDateTime } from '@/lib/formatDate'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

export function UsersPage() {
  const { data: users = [], isLoading } = useGetUsersQuery()
  const [createUser, { isLoading: creating }] = useCreateUserMutation()

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('Recruiter')
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState<string | null>(null)

  const onAdd = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    setSaved(null)
    try {
      const created = await createUser({ displayName, email, password, role }).unwrap()
      setSaved(`Created ${created.displayName} (${created.email}). They can sign in now.`)
      setDisplayName('')
      setEmail('')
      setPassword('')
      setRole('Recruiter')
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not create the user.')
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">Users</h2>
        <p className="text-sm text-muted-foreground">
          Add people who need their own login. Each user gets their own leads, campaigns, sequences, jobs and
          connections — they never see anyone else's data.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Add a user</CardTitle>
          <CardDescription>
            They sign in with this email and password, then set up their own Microsoft 365 and LinkedIn
            connections.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-4 sm:grid-cols-2" onSubmit={onAdd}>
            <div className="space-y-1.5">
              <Label>Name</Label>
              <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="e.g. Jane Smith" required />
            </div>
            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="jane@yourcompany.com" required />
            </div>
            <div className="space-y-1.5">
              <Label>Initial password</Label>
              <Input
                type="text"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="At least 8 characters"
                minLength={8}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label>Role</Label>
              <Select value={role} onChange={(e) => setRole(e.target.value)}>
                <option value="Recruiter">Recruiter</option>
                <option value="Admin">Administrator</option>
              </Select>
            </div>

            <div className="sm:col-span-2">
              {error && <p className="text-sm text-destructive">{error}</p>}
              {saved && <p className="text-sm text-emerald-600">{saved}</p>}
            </div>

            <div className="sm:col-span-2">
              <Button type="submit" disabled={creating}>
                {creating ? <Loader2 className="animate-spin" /> : <UserPlus />}
                Add user
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>All users ({users.length})</CardTitle>
          <CardDescription>Everyone who can sign in to this workspace.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading…
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Microsoft 365</TableHead>
                  <TableHead>Last signed in</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-medium">{user.displayName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <Badge variant={user.role === 'Admin' ? 'default' : 'secondary'}>{user.role}</Badge>
                    </TableCell>
                    <TableCell>
                      {user.microsoftConnected ? (
                        <Badge variant="success">Connected</Badge>
                      ) : (
                        <Badge variant="outline">Not connected</Badge>
                      )}
                    </TableCell>
                    <TableCell>{user.lastLoginAt ? formatDateTime(user.lastLoginAt) : '—'}</TableCell>
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
