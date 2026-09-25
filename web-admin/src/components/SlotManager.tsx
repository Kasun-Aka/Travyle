import { CalendarDays, Clock3, Pencil, Trash2, Users } from 'lucide-react'
import { useState } from 'react'
import './slot-manager.css'

type Schedule = {
  id: string
  destinationId?: string
  destinationTitle: string
  location: string
  guideName: string
  pricePerPerson: number
  maxCapacityPerSlot: number
  availableDates: string[]
  availableTimeSlots: string[]
  bookedSlotsMap: Record<string, number>
  slots?: { date: string; timeSlot: string; booked: number }[]
}

type Props = { schedules: Schedule[]; onCreated: (schedule: Schedule) => void }
type SlotSelection = { scheduleId: string; date: string; slot: string } | null

type FormState = { title: string; location: string; guide: string; price: string; capacity: string; dates: string; slots: string }

const API = import.meta.env.VITE_API_URL ?? 'http://localhost:5085/api'
const emptyForm: FormState = { title: '', location: '', guide: '', price: '', capacity: '8', dates: '', slots: '09:00 AM' }
const formatDate = (value: string) => new Date(value).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
const slotDateTime = (date: string, slot: string) => {
  const match = slot.trim().match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i)
  if (!match) return new Date(`${date.slice(0, 10)}T23:59:59`)
  let hour = Number(match[1])
  if (match[3].toUpperCase() === 'PM' && hour !== 12) hour += 12
  if (match[3].toUpperCase() === 'AM' && hour === 12) hour = 0
  return new Date(`${date.slice(0, 10)}T${String(hour).padStart(2, '0')}:${match[2]}:00`)
}
const slotKey = (date: string, slot: string) => `${date.slice(0, 10)}_${slot}`
const isFutureSlot = (date: string, slot: string) => slotDateTime(date, slot).getTime() > Date.now()

export default function SlotManager({ schedules: initialSchedules, onCreated }: Props) {
  const [schedules, setSchedules] = useState(initialSchedules)
  const [selected, setSelected] = useState<Schedule | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<SlotSelection>(null)
  const [notice, setNotice] = useState('')
  const [form, setForm] = useState<FormState>(emptyForm)
  const [saving, setSaving] = useState(false)

  const setField = (field: keyof FormState, value: string) => setForm(current => ({ ...current, [field]: value }))
  const formPayload = (id: string) => ({ destinationId: id, destinationTitle: form.title, location: form.location, guideName: form.guide, pricePerPerson: Number(form.price), maxCapacityPerSlot: Number(form.capacity), rating: 0, reviewsCount: 0, availableDates: form.dates.split(',').map(value => new Date(value.trim()).toISOString()), availableTimeSlots: form.slots.split(',').map(value => value.trim()) })

  const create = async (event: React.FormEvent) => {
    event.preventDefault()
    setSaving(true)
    const body = formPayload(crypto.randomUUID())
    try {
      const response = await fetch(`${API}/booking-schedules`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      if (!response.ok) throw new Error()
      const created = await response.json() as Schedule
      setSchedules(current => [created, ...current]); onCreated(created); setNotice('New booking experience published.')
    } catch {
      const created = { ...body, id: crypto.randomUUID(), bookedSlotsMap: {} }
      setSchedules(current => [created, ...current]); onCreated(created); setNotice('Experience saved in demo mode.')
    } finally { setSaving(false); setForm(emptyForm) }
  }

  const edit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!selected) return
    const slotSelection = selectedSlot
    if (slotSelection) {
      setSaving(true)
      try {
        const response = await fetch(`${API}/booking-schedules/${selected.id}/slots`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ date: slotSelection.date, timeSlot: slotSelection.slot, newDate: form.dates.split(',')[0].trim(), newTimeSlot: form.slots.split(',')[0].trim() }) })
        if (!response.ok) { const error = await response.json().catch(() => null) as { error?: string } | null; throw new Error(error?.error ?? 'Slot could not be edited.') }
        const updated = await response.json() as Schedule
        updated.slots = updated.slots?.map(slot => ({ ...slot, date: slot.date.slice(0, 10) }))
        setSchedules(current => current.map(item => item.id === updated.id ? updated : item)); setSelected(null); setSelectedSlot(null); setNotice('Slot updated.')
      } catch (error) { setNotice(error instanceof Error ? error.message : 'Slot could not be edited.') } finally { setSaving(false) }
      return
    }
    const dates = form.dates.split(',').map(value => new Date(value.trim()).toISOString())
    const slots = form.slots.split(',').map(value => value.trim())
    const body = { ...formPayload(selected.destinationId ?? selected.id), availableDates: dates, availableTimeSlots: slots }
    setSaving(true)
    try {
      const response = await fetch(`${API}/booking-schedules/${selected.id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      if (!response.ok) {
        const error = await response.json().catch(() => null) as { error?: string } | null
        throw new Error(error?.error ?? 'Schedule could not be edited.')
      }
      const updated = await response.json() as Schedule
      setSchedules(current => current.map(item => item.id === updated.id ? updated : item)); setSelected(null); setSelectedSlot(null); setNotice('Booking slot updated.')
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Schedule could not be edited.') } finally { setSaving(false) }
  }

  const openEdit = (schedule: Schedule, date?: string, slot?: string) => {
    setSelected(schedule)
    setSelectedSlot(date && slot ? { scheduleId: schedule.id, date, slot } : null)
    setForm({ title: schedule.destinationTitle, location: schedule.location, guide: schedule.guideName, price: String(schedule.pricePerPerson), capacity: String(schedule.maxCapacityPerSlot), dates: date ? date.slice(0, 10) : schedule.availableDates.map(item => item.slice(0, 10)).join(', '), slots: slot ?? schedule.availableTimeSlots.join(', ') })
  }

  const removeSlot = async (schedule: Schedule, date: string, slot: string) => {
    const booked = schedule.bookedSlotsMap[slotKey(date, slot)] ?? 0
    if (booked > 0) { setNotice('This slot has bookings and cannot be deleted.'); return }
    try {
      const response = await fetch(`${API}/booking-schedules/${schedule.id}/slots?date=${encodeURIComponent(date)}&timeSlot=${encodeURIComponent(slot)}`, { method: 'DELETE' })
      if (!response.ok) { const error = await response.json().catch(() => null) as { error?: string } | null; throw new Error(error?.error ?? 'Slot could not be deleted.') }
      const updated = await response.json() as Schedule
      setSchedules(current => current.map(item => item.id === updated.id ? updated : item)); setNotice(`${formatDate(date)} ${slot} removed.`)
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Slot could not be deleted.') }
  }

  return <div className="slot-page">
    <div className="slot-page-intro"><div><span className="slot-kicker">Inventory / Experiences</span><h2>Booking slots</h2><p>Publish and monitor every date, time slot, and capacity state.</p></div>{notice && <div className="slot-notice" role="status">{notice}</div>}</div>
    <div className="slot-layout">
      <form className="slot-form" onSubmit={selected ? edit : create}>
        <div className="slot-form-header"><div><span className="slot-kicker">{selectedSlot ? 'Individual slot' : selected ? 'Experience details' : 'New experience'}</span><h3>{selectedSlot ? 'Edit this slot' : selected ? 'Edit booking experience' : 'Create booking slots'}</h3></div><span className="slot-form-badge">{selected ? 'EDIT' : 'NEW'}</span></div>
        <label>Experience name<input required value={form.title} onChange={event => setField('title', event.target.value)} /></label>
        <label>Location<input required value={form.location} onChange={event => setField('location', event.target.value)} /></label>
        <label>Guide<input required value={form.guide} onChange={event => setField('guide', event.target.value)} /></label>
        <div className="slot-form-row"><label>Price per traveler<input required type="number" min="0" value={form.price} onChange={event => setField('price', event.target.value)} /></label><label>Capacity<input required type="number" min="1" value={form.capacity} onChange={event => setField('capacity', event.target.value)} /></label></div>
        <label>Date(s)<input required value={form.dates} onChange={event => setField('dates', event.target.value)} placeholder="2026-10-01, 2026-10-02" /></label>
        <label>Time slot(s)<input required value={form.slots} onChange={event => setField('slots', event.target.value)} placeholder="09:00 AM, 02:00 PM" /></label>
        <button className="slot-primary" disabled={saving}>{saving ? 'Saving...' : selected ? 'Save changes' : 'Publish experience'} <span>→</span></button>
        {selected && <button type="button" className="slot-secondary" onClick={() => { setSelected(null); setSelectedSlot(null); setForm(emptyForm) }}>Cancel edit</button>}
      </form>

      <section className="slot-results"><div className="slot-results-header"><div><span className="slot-kicker">Live inventory</span><h3>Saved booking experiences</h3></div><span className="slot-total">{schedules.length} experiences</span></div>
        <div className="experience-list">{schedules.map(schedule => <article className="experience-card" key={schedule.id}>
          <div className="experience-header"><div><span className="experience-location">{schedule.location}</span><h4>{schedule.destinationTitle}</h4><p>{schedule.guideName} · LKR {schedule.pricePerPerson.toLocaleString()} · capacity {schedule.maxCapacityPerSlot}</p></div></div>
          <div className="slot-date-grid">{(schedule.slots ?? schedule.availableDates.flatMap(date => schedule.availableTimeSlots.map(slot => ({ date, timeSlot: slot, booked: schedule.bookedSlotsMap[slotKey(date, slot)] ?? 0 })))).map(({ date, timeSlot: slot, booked }) => { const future = isFutureSlot(date, slot); return <div className={`slot-tile ${future ? 'upcoming' : 'past'}`} key={`${date}-${slot}`}><div className="slot-tile-top"><span><CalendarDays size={14} />{formatDate(date)}</span><span className="slot-state">{future ? 'Upcoming' : 'Started'}</span></div><strong><Clock3 size={17} />{slot}</strong><small><Users size={13} />{booked}/{schedule.maxCapacityPerSlot} booked</small><div className="slot-actions"><button type="button" onClick={() => openEdit(schedule, date, slot)} disabled={!future}><Pencil size={13} /> Edit</button><button type="button" onClick={() => removeSlot(schedule, date, slot)} disabled={!future || booked > 0}><Trash2 size={13} /> Delete</button></div></div> })}</div>
        </article>)}</div>
      </section>
    </div>
  </div>
}
