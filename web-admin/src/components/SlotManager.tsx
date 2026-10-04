import { CalendarDays, Clock3, Pencil, Trash2, Users } from 'lucide-react'
import { useState, useEffect } from 'react'

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
type GuideUser = { id: string; fullName: string; email: string; role: string }

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
  const [guides, setGuides] = useState<GuideUser[]>([])
  const [guidesLoading, setGuidesLoading] = useState(true)

  useEffect(() => {
    const fetchGuides = async () => {
      try {
        const response = await fetch(`${API}/auth/guides`)
        if (response.ok) {
          const data = await response.json() as GuideUser[]
          setGuides(data)
        }
      } catch {
        // API unavailable – fall back to free-text input
      } finally {
        setGuidesLoading(false)
      }
    }
    fetchGuides()
  }, [])

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

  return <div className="p-6">
    <div className="mb-6 flex justify-between items-start">
      <div>
        <span className="text-xs font-bold uppercase tracking-widest text-indigo-600">Inventory / Experiences</span>
        <h2 className="text-2xl font-black mt-1 mb-1">Booking slots</h2>
        <p className="text-sm text-slate-500 m-0">Publish and monitor every date, time slot, and capacity state.</p>
      </div>
      {notice && <div className="bg-indigo-50 border border-indigo-200 text-indigo-800 text-sm font-semibold px-4 py-2.5 rounded-lg max-w-[300px]" role="status">{notice}</div>}
    </div>
    
    <div className="grid grid-cols-1 lg:grid-cols-[380px_1fr] gap-8 items-start">
      <form className="bg-white border border-slate-200 rounded-2xl p-7 shadow-sm sticky top-6" onSubmit={selected ? edit : create}>
        <div className="flex justify-between items-center mb-6 pb-4 border-b border-slate-100">
          <div>
            <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 mb-1 block">
              {selectedSlot ? 'Individual slot' : selected ? 'Experience details' : 'New experience'}
            </span>
            <h3 className="text-lg font-bold m-0 text-slate-900">
              {selectedSlot ? 'Edit this slot' : selected ? 'Edit booking experience' : 'Create booking slots'}
            </h3>
          </div>
          <span className={`text-[10px] font-bold px-2 py-1 rounded-md tracking-wider ${selected ? 'bg-amber-100 text-amber-700' : 'bg-green-100 text-green-700'}`}>
            {selected ? 'EDIT' : 'NEW'}
          </span>
        </div>
        
        <div className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
            Experience name
            <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required value={form.title} onChange={event => setField('title', event.target.value)} />
          </label>
          <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
            Location
            <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required value={form.location} onChange={event => setField('location', event.target.value)} />
          </label>
          <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
            Guide
            {!guidesLoading && guides.length > 0 ? (
              <select
                className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium cursor-pointer"
                required
                value={form.guide}
                onChange={event => setField('guide', event.target.value)}
              >
                <option value="">— Select a registered guide —</option>
                {guides.map(g => (
                  <option key={g.id} value={g.fullName}>
                    {g.fullName} ({g.role})
                  </option>
                ))}
              </select>
            ) : (
              <input
                className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium"
                required
                placeholder={guidesLoading ? 'Loading guides…' : 'Enter guide name'}
                value={form.guide}
                onChange={event => setField('guide', event.target.value)}
              />
            )}
          </label>
          
          <div className="grid grid-cols-2 gap-4">
            <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
              Price per traveler
              <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required type="number" min="0" value={form.price} onChange={event => setField('price', event.target.value)} />
            </label>
            <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
              Capacity
              <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required type="number" min="1" value={form.capacity} onChange={event => setField('capacity', event.target.value)} />
            </label>
          </div>
          
          <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
            Date(s)
            <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required value={form.dates} onChange={event => setField('dates', event.target.value)} placeholder="2026-10-01, 2026-10-02" />
          </label>
          <label className="flex flex-col gap-1.5 text-[13px] font-bold text-slate-700">
            Time slot(s)
            <input className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-900 outline-none focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all font-inherit font-medium" required value={form.slots} onChange={event => setField('slots', event.target.value)} placeholder="09:00 AM, 02:00 PM" />
          </label>
          
          <button className="bg-indigo-600 text-white border-none py-3 px-5 rounded-lg font-bold text-[14px] cursor-pointer flex justify-center items-center gap-2 mt-4 hover:bg-indigo-700 hover:shadow-md transition-all active:translate-y-px disabled:opacity-70 disabled:cursor-wait" disabled={saving}>
            {saving ? 'Saving...' : selected ? 'Save changes' : 'Publish experience'} <span>→</span>
          </button>
          
          {selected && (
            <button type="button" className="bg-transparent border border-slate-200 text-slate-600 py-3 px-5 rounded-lg font-bold text-[14px] cursor-pointer hover:bg-slate-50 hover:border-slate-300 transition-colors" onClick={() => { setSelected(null); setSelectedSlot(null); setForm(emptyForm) }}>
              Cancel edit
            </button>
          )}
        </div>
      </form>

      <section className="flex flex-col gap-6">
        <div className="flex justify-between items-end pb-3 border-b border-slate-200">
          <div>
            <span className="text-[11px] font-bold uppercase tracking-widest text-slate-400">Live inventory</span>
            <h3 className="text-xl font-extrabold m-0 mt-1 text-slate-900">Saved booking experiences</h3>
          </div>
          <span className="text-sm font-semibold text-slate-500 bg-slate-100 px-3 py-1 rounded-full">{schedules.length} experiences</span>
        </div>
        
        <div className="flex flex-col gap-8">
          {schedules.map(schedule => (
            <article className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden" key={schedule.id}>
              <div className="bg-slate-50 px-6 py-5 border-b border-slate-200 flex justify-between items-start">
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-indigo-600 mb-1.5 block">{schedule.location}</span>
                  <h4 className="text-lg font-black m-0 text-slate-900">{schedule.destinationTitle}</h4>
                  <p className="text-sm text-slate-500 m-0 mt-1.5 font-medium">
                    {schedule.guideName} <span className="mx-1 text-slate-300">•</span> LKR {schedule.pricePerPerson.toLocaleString()} <span className="mx-1 text-slate-300">•</span> capacity {schedule.maxCapacityPerSlot}
                  </p>
                </div>
              </div>
              
              <div className="p-6 grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-4 bg-slate-50/50">
                {(schedule.slots ?? schedule.availableDates.flatMap(date => schedule.availableTimeSlots.map(slot => ({ date, timeSlot: slot, booked: schedule.bookedSlotsMap[slotKey(date, slot)] ?? 0 })))).map(({ date, timeSlot: slot, booked }) => { 
                  const future = isFutureSlot(date, slot); 
                  return (
                    <div className={`bg-white border rounded-xl p-4 flex flex-col gap-2 relative transition-all ${future ? 'border-slate-200 hover:border-indigo-300 hover:shadow-md' : 'border-slate-200 opacity-60 grayscale'}`} key={`${date}-${slot}`}>
                      <div className="flex justify-between items-center text-[12px] font-semibold text-slate-500 mb-1">
                        <span className="flex items-center gap-1.5"><CalendarDays size={14} />{formatDate(date)}</span>
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded uppercase tracking-wider ${future ? 'bg-indigo-50 text-indigo-600' : 'bg-slate-100 text-slate-500'}`}>
                          {future ? 'Upcoming' : 'Started'}
                        </span>
                      </div>
                      
                      <strong className="text-lg font-black flex items-center gap-1.5 text-slate-900">
                        <Clock3 size={17} className="text-indigo-600" />{slot}
                      </strong>
                      
                      <small className={`text-[13px] font-bold flex items-center gap-1.5 mb-3 ${booked >= schedule.maxCapacityPerSlot ? 'text-amber-600' : 'text-slate-600'}`}>
                        <Users size={13} />
                        {booked}/{schedule.maxCapacityPerSlot} booked
                      </small>
                      
                      <div className="flex gap-2 mt-auto border-t border-slate-100 pt-3">
                        <button 
                          type="button" 
                          className="flex-1 bg-slate-50 border border-slate-200 text-slate-600 py-1.5 px-0 rounded-md text-[11px] font-bold cursor-pointer flex justify-center items-center gap-1.5 hover:bg-white hover:border-indigo-200 hover:text-indigo-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed" 
                          onClick={() => openEdit(schedule, date, slot)} 
                          disabled={!future}
                        >
                          <Pencil size={13} /> Edit
                        </button>
                        <button 
                          type="button" 
                          className="flex-1 bg-slate-50 border border-slate-200 text-slate-600 py-1.5 px-0 rounded-md text-[11px] font-bold cursor-pointer flex justify-center items-center gap-1.5 hover:bg-red-50 hover:border-red-200 hover:text-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed" 
                          onClick={() => removeSlot(schedule, date, slot)} 
                          disabled={!future || booked > 0}
                        >
                          <Trash2 size={13} /> Delete
                        </button>
                      </div>
                    </div>
                  ) 
                })}
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  </div>
}
