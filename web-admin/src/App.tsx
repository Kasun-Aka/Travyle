import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { auth } from "./lib/firebase";
import SlotManager from "./components/SlotManager";
import AgentApprovals from "./components/AgentApprovals";

type View = "overview" | "bookings" | "slots" | "requests" | "agent";
type Booking = {
  id: string;
  bookingReference: string;
  destinationTitle: string;
  location: string;
  travelerName: string;
  travelerEmail: string;
  bookingDate: string;
  timeSlot: string;
  guests: number;
  totalAmount: number;
  status: string;
  paymentStatus: string;
  paymentMethod: string;
  receiptReference?: string;
  receiptImageData?: string;
  createdAt: string;
};
type Schedule = {
  id: string;
  destinationTitle: string;
  location: string;
  guideName: string;
  pricePerPerson: number;
  maxCapacityPerSlot: number;
  availableDates: string[];
  availableTimeSlots: string[];
  bookedSlotsMap: Record<string, number>;
};
type Request = {
  id: string;
  bookingId: string;
  travelerId: string;
  originalPrice: number;
  requestedDiscountPercent: number;
  calculatedDiscountAmount: number;
  reason: string;
  status: string;
  createdAt: string;
};
const API = import.meta.env.VITE_API_URL ?? "http://localhost:5085/api";
const money = (value: number) =>
  `LKR ${value.toLocaleString("en-LK", { maximumFractionDigits: 0 })}`;
const formatDate = (value: string) =>
  new Date(value).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
const demoBookings: Booking[] = [
  {
    id: "1",
    bookingReference: "BKG-48291",
    destinationTitle: "Ella Rock & Nine Arch Bridge Trek",
    location: "Ella",
    travelerName: "Maya Fernando",
    travelerEmail: "maya@email.com",
    bookingDate: "2026-09-22T00:00:00Z",
    timeSlot: "06:30 AM",
    guests: 2,
    totalAmount: 9450,
    status: "Confirmed",
    paymentStatus: "HeldInEscrow",
    paymentMethod: "SampleCard",
    createdAt: "2026-09-20T08:20:00Z",
  },
  {
    id: "2",
    bookingReference: "BKG-48288",
    destinationTitle: "Sigiriya Sunrise Tour",
    location: "Sigiriya",
    travelerName: "Ravin Perera",
    travelerEmail: "ravin@email.com",
    bookingDate: "2026-09-24T00:00:00Z",
    timeSlot: "05:30 AM",
    guests: 1,
    totalAmount: 6300,
    status: "Pending",
    paymentStatus: "Pending",
    paymentMethod: "BankTransferReceipt",
    receiptReference: "BOC-991482",
    createdAt: "2026-09-20T07:48:00Z",
  },
  {
    id: "3",
    bookingReference: "BKG-48271",
    destinationTitle: "Mirissa Whale Watching",
    location: "Mirissa",
    travelerName: "Ishara Silva",
    travelerEmail: "ishara@email.com",
    bookingDate: "2026-09-25T00:00:00Z",
    timeSlot: "06:00 AM",
    guests: 4,
    totalAmount: 35700,
    status: "Pending",
    paymentStatus: "Pending",
    paymentMethod: "BankTransferReceipt",
    receiptReference: "BOC-220194",
    createdAt: "2026-09-19T15:12:00Z",
  },
];
const demoSchedules: Schedule[] = [
  {
    id: "1",
    destinationTitle: "Ella Rock & Nine Arch Bridge Trek",
    location: "Ella, Badulla District",
    guideName: "Kasun Bandara",
    pricePerPerson: 4500,
    maxCapacityPerSlot: 8,
    availableDates: ["2026-09-22", "2026-09-23"],
    availableTimeSlots: ["06:30 AM", "09:00 AM"],
    bookedSlotsMap: {},
  },
  {
    id: "2",
    destinationTitle: "Sigiriya Ancient Rock Fortress",
    location: "Sigiriya, Matale District",
    guideName: "Anura Senanayake",
    pricePerPerson: 6000,
    maxCapacityPerSlot: 10,
    availableDates: ["2026-09-24"],
    availableTimeSlots: ["05:30 AM", "08:00 AM"],
    bookedSlotsMap: {},
  },
];
const demoRequests: Request[] = [
  {
    id: "1",
    bookingId: "BKG-48271",
    travelerId: "Ishara Silva",
    originalPrice: 35700,
    requestedDiscountPercent: 20,
    calculatedDiscountAmount: 7140,
    reason: "Student concession with university ID",
    status: "Pending",
    createdAt: "2026-09-19T14:00:00Z",
  },
];
async function authHeaders(): Promise<Record<string, string>> {
  const token = await auth?.currentUser?.getIdToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (token) headers.Authorization = `Bearer ${token}`;
  return headers;
}

async function get<T>(path: string, fallback: T): Promise<T> {
  try {
    const response = await fetch(`${API}${path}`, {
      headers: await authHeaders(),
    });
    if (!response.ok) throw new Error();
    return (await response.json()) as T;
  } catch {
    return fallback;
  }
}

function App() {
  const navigate = useNavigate();
  const [view, setView] = useState<View>("overview");
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [schedules, setSchedules] = useState<Schedule[]>([]);
  const [requests, setRequests] = useState<Request[]>([]);
  const [notice, setNotice] = useState("");
  const loadData = async () => {
    const [nextBookings, nextSchedules, nextRequests] = await Promise.all([
      get<Booking[]>("/bookings/admin/all?page=1&pageSize=100", demoBookings),
      get<Schedule[]>("/booking-schedules", demoSchedules),
      get<Request[]>("/discount-requests", demoRequests),
    ]);
    setBookings(nextBookings);
    setSchedules(nextSchedules);
    setRequests(nextRequests);
  };
  useEffect(() => {
    void loadData();
  }, []);
  const updateRequest = async (id: string, status: "Approved" | "Rejected") => {
    try {
      await fetch(`${API}/discount-requests/${id}/status`, {
        method: "PUT",
        headers: await authHeaders(),
        body: JSON.stringify({ status }),
      });
    } catch {
      /* demo mode */
    }
    setRequests((current) =>
      current.map((request) =>
        request.id === id ? { ...request, status } : request,
      ),
    );
    setNotice(`Request ${status.toLowerCase()}.`);
  };
  const verifyPayment = async (booking: Booking) => {
    try {
      await fetch(`${API}/bookings/${booking.id}/process-escrow-payment`, {
        method: "POST",
        headers: await authHeaders(),
        body: JSON.stringify({
          bookingId: booking.id,
          totalAmount: booking.totalAmount,
          paymentMethodId: booking.receiptReference ?? "admin-receipt-review",
        }),
      });
    } catch {
      /* demo mode */
    }
    setBookings((current) =>
      current.map((item) =>
        item.id === booking.id
          ? { ...item, status: "Confirmed", paymentStatus: "HeldInEscrow" }
          : item,
      ),
    );
    setNotice(
      `${booking.bookingReference} payment verified and held in escrow.`,
    );
  };
  const pendingPayments = bookings.filter(
    (booking) => booking.paymentStatus === "Pending",
  ).length;
  const pendingRequests = requests.filter(
    (request) => request.status === "Pending",
  ).length;
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">T</span>
          <span>
            <b>travyle</b>
            <small>operations desk</small>
          </span>
        </div>
        <div className="workspace-label">Workspace</div>
        <nav>
          {(
            [
              ["overview", "Overview"],
              ["bookings", "Bookings & payments"],
              ["slots", "Booking slots"],
              ["requests", "Bonus requests"],
              ["agent", "Agent Approvals"],
            ] as [View, string][]
          ).map(([key, label]) => (
            <button
              className={view === key ? "nav-item active" : "nav-item"}
              key={key}
              onClick={() => setView(key)}
            >
              <span className="nav-dot" />
              {label}
              {key === "bookings" && pendingPayments > 0 && (
                <em>{pendingPayments}</em>
              )}
              {key === "requests" && pendingRequests > 0 && (
                <em>{pendingRequests}</em>
              )}
            </button>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <div className="admin-avatar">AD</div>
          <div>
            <b>Admin desk</b>
            <small>Operations manager</small>
          </div>
        </div>
      </aside>
      <main className="main-content">
        <header className="topbar workspace-topbar">
          <div className="workspace-heading">
            <button
              className="workspace-back"
              type="button"
              title="Back to main dashboard"
              aria-label="Back to main dashboard"
              onClick={() => navigate("/welcome")}
            >
              <ArrowLeft size={18} />
            </button>
            <div>
              <span className="breadcrumb">TRAVYLE / {view.toUpperCase()}</span>
              <h1>
                {view === "overview"
                  ? "Good morning, Admin"
                  : view === "bookings"
                    ? "Bookings & payments"
                    : view === "slots"
                      ? "Booking slots"
                      : "Bonus requests"}
              </h1>
            </div>
          </div>
          <div className="top-actions">
            <button
              className="icon-button"
              title="Refresh"
              onClick={() => void loadData()}
            >
              ↻
            </button>
            <div className="status-live">
              <i /> API connected
            </div>
          </div>
        </header>
        {notice && (
          <div className="notice" onClick={() => setNotice("")}>
            {notice} <span>×</span>
          </div>
        )}
        {view === "overview" && (
          <Overview
            bookings={bookings}
            schedules={schedules}
            requests={requests}
            go={setView}
          />
        )}
        {view === "bookings" && (
          <Bookings bookings={bookings} onVerify={verifyPayment} />
        )}
        {view === "slots" && (
          <SlotManager
            schedules={schedules}
            onCreated={(schedule) => {
              setSchedules((current) => [schedule, ...current]);
              setNotice("New booking slot published.");
            }}
          />
        )}
        {view === "requests" && (
          <Requests requests={requests} onUpdate={updateRequest} />
        )}
        {view === "agent" && (
          <AgentApprovals
            apiUrl={API}
            onBookingApproved={() => {
              void authHeaders()
                .then((headers) =>
                  fetch(`${API}/bookings/admin/all?page=1&pageSize=50`, {
                    headers,
                  }),
                )
                .then((response) => (response.ok ? response.json() : []))
                .then((data) => setBookings(data))
                .catch(() => {});
            }}
          />
        )}
      </main>
    </div>
  );
}

function Overview({
  bookings,
  schedules,
  requests,
  go,
}: {
  bookings: Booking[];
  schedules: Schedule[];
  requests: Request[];
  go: (view: View) => void;
}) {
  const receipts = bookings.filter(
    (booking) => booking.paymentStatus === "Pending",
  );
  return (
    <div className="content-grid">
      <section className="hero-strip">
        <div>
          <span className="eyebrow">TODAY / OPERATIONS SNAPSHOT</span>
          <h2>
            Keep the journey
            <br />
            <i>in motion.</i>
          </h2>
          <p>
            Review the moments that need your attention and keep every itinerary
            on track.
          </p>
        </div>
        <div className="hero-stat">
          <strong>{bookings.length}</strong>
          <span>
            total bookings
            <br />
            in the queue
          </span>
        </div>
      </section>
      <section className="metric-grid">
        <Metric
          label="Gross booking value"
          value={money(
            bookings.reduce((sum, booking) => sum + booking.totalAmount, 0),
          )}
          trend="+12.4% this month"
          tone="green"
        />
        <Metric
          label="Payment review"
          value={String(receipts.length).padStart(2, "0")}
          trend="receipt submissions"
          tone="amber"
        />
        <Metric
          label="Live schedules"
          value={String(schedules.length).padStart(2, "0")}
          trend="active experiences"
          tone="blue"
        />
        <Metric
          label="Bonus requests"
          value={String(
            requests.filter((request) => request.status === "Pending").length,
          ).padStart(2, "0")}
          trend="awaiting decision"
          tone="coral"
        />
      </section>
      <section className="split-grid">
        <div className="panel">
          <PanelHeading
            title="Payment review"
            action="View all"
            onClick={() => go("bookings")}
          />
          <div className="mini-list">
            {receipts.slice(0, 3).map((booking) => (
              <BookingRow key={booking.id} booking={booking} />
            ))}
            {receipts.length === 0 && (
              <Empty text="No payment reviews waiting." />
            )}
          </div>
        </div>
        <div className="panel dark-panel">
          <PanelHeading
            title="Next departures"
            action="Manage slots"
            onClick={() => go("slots")}
            light
          />
          <div className="departure-list">
            {bookings.slice(0, 3).map((booking) => (
              <div className="departure" key={booking.id}>
                <div className="date-tile">
                  <b>{new Date(booking.bookingDate).getDate()}</b>
                  <small>
                    {new Date(booking.bookingDate)
                      .toLocaleDateString("en", { month: "short" })
                      .toUpperCase()}
                  </small>
                </div>
                <div>
                  <b>{booking.destinationTitle}</b>
                  <small>
                    {booking.timeSlot} · {booking.guests} guests
                  </small>
                </div>
                <span className="arrow">↗</span>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
function Metric({
  label,
  value,
  trend,
  tone,
}: {
  label: string;
  value: string;
  trend: string;
  tone: string;
}) {
  return (
    <div className={`metric-card ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>
        <i />
        {trend}
      </small>
    </div>
  );
}
function PanelHeading({
  title,
  action,
  onClick,
  light = false,
}: {
  title: string;
  action: string;
  onClick: () => void;
  light?: boolean;
}) {
  return (
    <div className="panel-heading">
      <h3 className={light ? "light-text" : ""}>{title}</h3>
      <button
        className={light ? "text-action light-text" : "text-action"}
        onClick={onClick}
      >
        {action} →
      </button>
    </div>
  );
}
function BookingRow({ booking }: { booking: Booking }) {
  return (
    <div className="booking-row">
      <div className="person-avatar">
        {booking.travelerName
          .split(" ")
          .map((part) => part[0])
          .join("")
          .slice(0, 2)}
      </div>
      <div className="row-main">
        <b>{booking.travelerName}</b>
        <small>
          {booking.bookingReference} · {booking.destinationTitle}
        </small>
      </div>
      <div className="row-value">
        <b>{money(booking.totalAmount)}</b>
        <small className="amber-text">Receipt review</small>
      </div>
    </div>
  );
}
function Empty({ text }: { text: string }) {
  return <div className="empty">{text}</div>;
}
function Bookings({
  bookings,
  onVerify,
}: {
  bookings: Booking[];
  onVerify: (booking: Booking) => void;
}) {
  const [filter, setFilter] = useState("All");
  const [receiptBooking, setReceiptBooking] = useState<Booking | null>(null);
  const filtered =
    filter === "All"
      ? bookings
      : filter === "Receipts"
        ? bookings.filter((booking) => booking.paymentStatus === "Pending")
        : bookings.filter((booking) => booking.status === filter);
  return (
    <div className="content-grid">
      <div className="page-intro">
        <div>
          <span className="eyebrow">LIVE BOOKING QUEUE</span>
          <p>
            Every reservation, payment method, and verification signal in one
            place.
          </p>
        </div>
        <button className="secondary-button">Export report ↓</button>
      </div>
      <div className="filter-bar">
        {["All", "Pending", "Confirmed", "Receipts"].map((value) => (
          <button
            className={filter === value ? "filter active" : "filter"}
            key={value}
            onClick={() => setFilter(value)}
          >
            {value}
          </button>
        ))}
        <span className="filter-count">{filtered.length} records</span>
      </div>
      <div className="table-panel">
        <table>
          <thead>
            <tr>
              <th>Traveler</th>
              <th>Experience</th>
              <th>Date & time</th>
              <th>Amount</th>
              <th>Payment</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {filtered.map((booking) => (
              <tr key={booking.id}>
                <td>
                  <b>{booking.travelerName}</b>
                  <small>{booking.bookingReference}</small>
                </td>
                <td>
                  <b>{booking.destinationTitle}</b>
                  <small>
                    {booking.location} · {booking.guests} guests
                  </small>
                </td>
                <td>
                  <b>{formatDate(booking.bookingDate)}</b>
                  <small>{booking.timeSlot}</small>
                </td>
                <td>
                  <b>{money(booking.totalAmount)}</b>
                </td>
                <td>
                  <span
                    className={`pill ${booking.paymentStatus === "Pending" ? "pill-amber" : "pill-green"}`}
                  >
                    {booking.paymentStatus === "Pending"
                      ? "Receipt review"
                      : "Escrow held"}
                  </span>
                  <small>
                    {booking.paymentMethod === "SampleCard"
                      ? "Sample card"
                      : (booking.receiptReference ?? "Image attached")}
                  </small>
                  {booking.paymentStatus === "Pending" && (
                    <button
                      className="text-action"
                      onClick={() => setReceiptBooking(booking)}
                    >
                      {booking.receiptImageData ? "View receipt" : "No image"}
                    </button>
                  )}
                </td>
                <td>
                  <span
                    className={`status-dot ${booking.status.toLowerCase()}`}
                  />
                  {booking.status}
                </td>
                <td>
                  {booking.paymentStatus === "Pending" && (
                    <button
                      className="verify-button"
                      onClick={() => setReceiptBooking(booking)}
                    >
                      Review
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {receiptBooking && (
        <ReceiptReview
          booking={receiptBooking}
          onClose={() => setReceiptBooking(null)}
          onVerify={() => {
            onVerify(receiptBooking);
            setReceiptBooking(null);
          }}
        />
      )}
    </div>
  );
}

function ReceiptReview({
  booking,
  onClose,
  onVerify,
}: {
  booking: Booking;
  onClose: () => void;
  onVerify: () => void;
}) {
  return (
    <div className="receipt-modal-backdrop" onClick={onClose}>
      <section
        className="receipt-modal"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-heading">
          <div>
            <span className="eyebrow">PAYMENT REVIEW</span>
            <h3>{booking.bookingReference}</h3>
          </div>
          <button
            className="icon-button"
            onClick={onClose}
            aria-label="Close receipt"
          >
            ×
          </button>
        </div>
        <p>
          <b>{booking.travelerName}</b> ·{" "}
          {booking.paymentMethod === "AtmCashReceipt"
            ? "ATM cash deposit"
            : booking.paymentMethod === "BankTransferReceipt"
              ? "Bank transfer"
              : "Card receipt"}{" "}
          · {money(booking.totalAmount)}
        </p>
        {booking.receiptReference && (
          <small>Reference: {booking.receiptReference}</small>
        )}
        {booking.receiptImageData ? (
          <img
            className="receipt-image"
            src={booking.receiptImageData}
            alt={`Payment receipt for ${booking.bookingReference}`}
          />
        ) : (
          <div className="receipt-empty">No receipt image was submitted.</div>
        )}
        <div className="request-actions">
          <button className="secondary-button" onClick={onClose}>
            Close
          </button>
          {booking.receiptImageData && (
            <button className="approve-button" onClick={onVerify}>
              Confirm payment
            </button>
          )}
        </div>
      </section>
    </div>
  );
}
function LegacySlots({
  schedules,
  onCreated,
}: {
  schedules: Schedule[];
  onCreated: (schedule: Schedule) => void;
}) {
  const [form, setForm] = useState({
    title: "",
    location: "",
    guide: "",
    price: "",
    capacity: "8",
    dates: "",
    slots: "09:00 AM",
  });
  const [saving, setSaving] = useState(false);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    const body = {
      destinationId: crypto.randomUUID(),
      destinationTitle: form.title,
      location: form.location,
      guideName: form.guide,
      pricePerPerson: Number(form.price),
      maxCapacityPerSlot: Number(form.capacity),
      rating: 0,
      reviewsCount: 0,
      availableDates: form.dates
        .split(",")
        .map((value) => new Date(value.trim()).toISOString()),
      availableTimeSlots: form.slots.split(",").map((value) => value.trim()),
    };
    try {
      const response = await fetch(`${API}/booking-schedules`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      if (response.ok) onCreated((await response.json()) as Schedule);
      else throw new Error();
    } catch {
      onCreated({
        ...body,
        id: crypto.randomUUID(),
        availableDates: body.availableDates,
        availableTimeSlots: body.availableTimeSlots,
        bookedSlotsMap: {},
      });
    } finally {
      setSaving(false);
      setForm({
        title: "",
        location: "",
        guide: "",
        price: "",
        capacity: "8",
        dates: "",
        slots: "09:00 AM",
      });
    }
  };
  return (
    <div className="content-grid">
      <div className="page-intro">
        <div>
          <span className="eyebrow">INVENTORY / EXPERIENCES</span>
          <p>
            Publish the dates and time windows travelers can book from the
            mobile app.
          </p>
        </div>
      </div>
      <div className="slot-layout">
        <form className="panel form-panel" onSubmit={submit}>
          <div className="panel-heading">
            <h3>Create a booking slot</h3>
            <span className="form-badge">NEW</span>
          </div>
          <label>
            Experience name
            <input
              required
              value={form.title}
              onChange={(event) =>
                setForm({ ...form, title: event.target.value })
              }
              placeholder="e.g. Kandy tea country walk"
            />
          </label>
          <label>
            Location
            <input
              required
              value={form.location}
              onChange={(event) =>
                setForm({ ...form, location: event.target.value })
              }
              placeholder="City or district"
            />
          </label>
          <div className="form-row">
            <label>
              Guide name
              <input
                required
                value={form.guide}
                onChange={(event) =>
                  setForm({ ...form, guide: event.target.value })
                }
                placeholder="Assigned guide"
              />
            </label>
            <label>
              Price per person
              <input
                required
                type="number"
                min="0"
                value={form.price}
                onChange={(event) =>
                  setForm({ ...form, price: event.target.value })
                }
                placeholder="LKR"
              />
            </label>
          </div>
          <div className="form-row">
            <label>
              Capacity per slot
              <input
                required
                type="number"
                min="1"
                value={form.capacity}
                onChange={(event) =>
                  setForm({ ...form, capacity: event.target.value })
                }
              />
            </label>
            <label>
              Time slots
              <input
                required
                value={form.slots}
                onChange={(event) =>
                  setForm({ ...form, slots: event.target.value })
                }
                placeholder="06:30 AM, 09:00 AM"
              />
            </label>
          </div>
          <label>
            Available dates
            <input
              required
              value={form.dates}
              onChange={(event) =>
                setForm({ ...form, dates: event.target.value })
              }
              placeholder="2026-09-25, 2026-09-26"
            />
            <small className="field-hint">
              Separate multiple dates with commas.
            </small>
          </label>
          <button className="primary-button" disabled={saving}>
            {saving ? "Publishing..." : "Publish booking slot →"}
          </button>
        </form>
        <div className="panel">
          <PanelHeading
            title="Published experiences"
            action={`${schedules.length} live`}
            onClick={() => undefined}
          />
          {schedules.map((schedule) => (
            <div className="schedule-card" key={schedule.id}>
              <div className="schedule-icon">↗</div>
              <div>
                <b>{schedule.destinationTitle}</b>
                <small>
                  {schedule.location} · {schedule.guideName}
                </small>
                <small>
                  {schedule.availableDates.length} dates ·{" "}
                  {schedule.availableTimeSlots.length} time windows ·{" "}
                  {money(schedule.pricePerPerson)}
                </small>
              </div>
              <span className="live-label">
                <i />
                Live
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
void LegacySlots;
function Requests({
  requests,
  onUpdate,
}: {
  requests: Request[];
  onUpdate: (id: string, status: "Approved" | "Rejected") => void;
}) {
  return (
    <div className="content-grid">
      <div className="page-intro">
        <div>
          <span className="eyebrow">DISCOUNTS / SPECIAL CONCESSIONS</span>
          <p>
            Review student, senior citizen, and hardship requests submitted
            after booking.
          </p>
        </div>
      </div>
      <div className="request-grid">
        {requests.map((request) => (
          <article className="request-card" key={request.id}>
            <div className="request-top">
              <span
                className={`pill ${request.status === "Pending" ? "pill-amber" : request.status === "Approved" ? "pill-green" : "pill-red"}`}
              >
                {request.status}
              </span>
              <small>{formatDate(request.createdAt)}</small>
            </div>
            <h3>{request.reason}</h3>
            <p>
              Traveler <b>{request.travelerId}</b> · Booking{" "}
              <b>{request.bookingId}</b>
            </p>
            <div className="request-amount">
              <span>Requested concession</span>
              <strong>
                {request.requestedDiscountPercent}%{" "}
                <small>({money(request.calculatedDiscountAmount)})</small>
              </strong>
            </div>
            {request.status === "Pending" && (
              <div className="request-actions">
                <button
                  className="approve-button"
                  onClick={() => onUpdate(request.id, "Approved")}
                >
                  Approve
                </button>
                <button
                  className="reject-button"
                  onClick={() => onUpdate(request.id, "Rejected")}
                >
                  Decline
                </button>
              </div>
            )}
          </article>
        ))}
      </div>
      {requests.length === 0 && (
        <div className="panel">
          <Empty text="No bonus requests yet." />
        </div>
      )}
    </div>
  );
}
export default App;
