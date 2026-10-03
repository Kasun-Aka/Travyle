import re
import uuid
from datetime import datetime, date, timedelta, timezone
from typing import TypedDict, List, Dict, Optional, Any
from langgraph.graph import StateGraph, END

from ..schemas.booking_schemas import (
    StartAgentBookingRequest,
    AgentWorkflowResponse,
    ProposedBookingDto,
)
from ..tools.backend_client import backend_client

# Sri Lanka timezone offset (UTC+5:30)
SL_OFFSET = timedelta(hours=5, minutes=30)
SL_TZ = timezone(SL_OFFSET)


def now_sl() -> datetime:
    """Return current datetime in Sri Lanka local time (UTC+5:30)."""
    return datetime.now(SL_TZ).replace(tzinfo=None)

# ============================================================================
# Graph State Definition
# ============================================================================

class BookingWorkflowState(TypedDict):
    workflow_id: str
    traveler_id: str
    traveler_name: str
    traveler_email: str
    objective: str
    preferred_schedule_id: Optional[str]
    preferred_date: Optional[str]
    preferred_time_slot: Optional[str]
    requested_guests: Optional[int]

    # Lifecycle state
    status: str  # "Running", "PendingApproval", "Completed", "Failed"
    plan: List[str]
    completed_steps: List[str]
    tool_results: Dict[str, Any]
    validation_results: Dict[str, bool]

    # Intermediary intent & tool values
    destination_query: Optional[str]
    explicit_date: Optional[str]
    explicit_slot: Optional[str]
    guests: int
    selected_schedule: Optional[Dict[str, Any]]
    target_date: Optional[str]
    target_slot: Optional[str]
    pricing_summary: Optional[Dict[str, Any]]
    proposed_booking: Optional[Dict[str, Any]]

    # Approval & execution
    approval_status: str  # "PENDING", "APPROVED", "REJECTED"
    approved_by: Optional[str]
    approver_role: Optional[str]
    approver_notes: Optional[str]
    approved_at: Optional[str]
    created_booking_id: Optional[str]
    booking_reference: Optional[str]
    error_message: Optional[str]

    created_at: str
    updated_at: str


# ============================================================================
# Security Guardrails & Deterministic Helpers
# ============================================================================

INJECTION_PATTERNS = [
    r"\bignore\s+(?:(?:all|previous|prior)\s+)*(?:previous\s+)?instructions\b",
    r"\bsystem\s+prompt\b",
    r"\bbypass\s+(?:approval|validation|security|guardrails)\b",
    r"\bconfirm\s+without\s+approval\b",
    r"\bgrant\s+admin\b",
    r"\bdrop\s+table\b",
    r"\bdelete\s+from\s+bookings\b",
    r"\bupdate\s+bookings\s+set\b",
    r"<script.*?>",
    r"--\s*$",
]

KNOWN_DESTINATIONS = [
    "Ella Rock", "Sigiriya", "Mirissa", "Yala", "Kandy", "Galle",
    "Nuwara Eliya", "Trincomalee", "Adam's Peak", "Pinnawala",
    "Sinharaja", "Bentota", "Colombo", "Dambulla", "Anuradhapura", "Polonnaruwa"
]
NUMBER_WORDS = {
    "one": 1,
    "two": 2,
    "three": 3,
    "four": 4,
    "five": 5,
    "six": 6,
    "seven": 7,
    "eight": 8,
    "nine": 9,
    "ten": 10,
}

def detect_prompt_injection(text: str) -> bool:
    for pat in INJECTION_PATTERNS:
        if re.search(pat, text, re.IGNORECASE):
            return True
    return False


def classify_conversation(text: str) -> Optional[str]:
    normalized = text.strip().lower()
    if re.fullmatch(r"(?:hello|hi|hey|good morning|good afternoon|good evening)", normalized):
        return "Hello! I can help you find available tours and booking schedules. What would you like to book?"
    if re.fullmatch(r"(?:thanks|thank you|okay|ok|bye|goodbye)", normalized):
        return "You're welcome! I'm happy to help."
    if re.search(r"\b(?:assign|book)\b.*\b(?:guide|tour guide)\b", normalized):
        return "I'm sorry, I can currently help with available tours, schedules and booking-related requests. I can't safely handle that request."
    return None


# Month name → number mapping for natural-language date parsing
_MONTH_MAP = {
    "january": 1, "jan": 1, "february": 2, "feb": 2,
    "march": 3, "mar": 3, "april": 4, "apr": 4,
    "may": 5, "june": 6, "jun": 6, "july": 7, "jul": 7,
    "august": 8, "aug": 8, "september": 9, "sep": 9, "sept": 9,
    "october": 10, "oct": 10, "november": 11, "nov": 11,
    "december": 12, "dec": 12,
}

_WEEKDAY_MAP = {
    "monday": 0, "tuesday": 1, "wednesday": 2, "thursday": 3,
    "friday": 4, "saturday": 5, "sunday": 6,
}


def _parse_natural_date(text: str) -> Optional[str]:
    """Try to parse a natural-language date from text. Returns YYYY-MM-DD or None."""
    now = now_sl()
    today = now.date()

    # Relative keywords
    if re.search(r"\bday after tomorrow\b", text, re.IGNORECASE):
        return (today + timedelta(days=2)).strftime("%Y-%m-%d")
    if re.search(r"\btomorrow\b", text, re.IGNORECASE):
        return (today + timedelta(days=1)).strftime("%Y-%m-%d")
    if re.search(r"\btoday\b", text, re.IGNORECASE):
        return today.strftime("%Y-%m-%d")

    # ISO format: 2026-10-05
    m = re.search(r"\b(20\d{2}-\d{2}-\d{2})\b", text)
    if m:
        return m.group(1)

    # "next <weekday>" e.g. "next Monday"
    m = re.search(r"\bnext\s+(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b",
                  text, re.IGNORECASE)
    if m:
        target_wd = _WEEKDAY_MAP[m.group(1).lower()]
        days_ahead = (target_wd - today.weekday() + 7) % 7 or 7
        return (today + timedelta(days=days_ahead)).strftime("%Y-%m-%d")

    # "October 5" or "5 October" or "5th October" or "Oct 5"
    month_pattern = "|".join(_MONTH_MAP.keys())
    m = re.search(
        rf"\b({month_pattern})\s+(\d{{1,2}})(?:st|nd|rd|th)?\b",
        text, re.IGNORECASE
    )
    if not m:
        m = re.search(
            rf"\b(\d{{1,2}})(?:st|nd|rd|th)?\s+({month_pattern})\b",
            text, re.IGNORECASE
        )
        if m:
            # swap group order — day is group(1), month is group(2)
            day_num = int(m.group(1))
            month_num = _MONTH_MAP[m.group(2).lower()]
        else:
            day_num = None
            month_num = None
    else:
        month_num = _MONTH_MAP[m.group(1).lower()]
        day_num = int(m.group(2))

    if day_num and month_num:
        year = today.year
        try:
            candidate = date(year, month_num, day_num)
        except ValueError:
            return None
        # If date already passed this year, try next year
        if candidate < today:
            try:
                candidate = date(year + 1, month_num, day_num)
            except ValueError:
                return None
        return candidate.strftime("%Y-%m-%d")

    # DD/MM/YYYY or D/M/YYYY
    m = re.search(r"\b(\d{1,2})/(\d{1,2})/(20\d{2})\b", text)
    if m:
        try:
            candidate = date(int(m.group(3)), int(m.group(2)), int(m.group(1)))
            return candidate.strftime("%Y-%m-%d")
        except ValueError:
            pass

    return None


def _parse_time_slot(text: str) -> Optional[str]:
    """Parse a time slot from text, returning a normalised '09:00 AM' style string or None."""
    # Explicit HH:MM AM/PM (with optional space)
    m = re.search(r"\b(\d{1,2}):(\d{2})\s*(AM|PM)\b", text, re.IGNORECASE)
    if m:
        return f"{int(m.group(1)):02d}:{m.group(2)} {m.group(3).upper()}"
    # Bare hour like "9 AM", "10am"
    m = re.search(r"\b(\d{1,2})\s*(AM|PM)\b", text, re.IGNORECASE)
    if m:
        return f"{int(m.group(1)):02d}:00 {m.group(2).upper()}"
    # Natural
    if re.search(r"\bmorning\b", text, re.IGNORECASE):
        return "09:00 AM"
    if re.search(r"\bafternoon\b", text, re.IGNORECASE):
        return "02:00 PM"
    if re.search(r"\bevening\b", text, re.IGNORECASE):
        return "05:00 PM"
    return None


def parse_intent_from_objective(
    objective: str,
    req_destination_id: Optional[str],
    req_date: Optional[datetime],
    req_slot: Optional[str],
    req_guests: Optional[int],
) -> Dict[str, Any]:
    # Extract destination
    destination_kw = None
    for dest in KNOWN_DESTINATIONS:
        if re.search(rf"\b{re.escape(dest)}\b", objective, re.IGNORECASE):
            destination_kw = dest
            break

    if not destination_kw:
        booking_text = re.sub(
            r"^\s*(?:i|we)\s+(?:want|would like|need)\s+to\s+",
            "",
            objective,
            flags=re.IGNORECASE,
        )
        match = re.search(r"\b(?:book|reserve)\s+(.+)", booking_text, re.IGNORECASE)
        if match:
            candidate = match.group(1).strip(" .,!?;:")
            candidate = re.sub(r"^(?:for|a|an|the)\s+", "", candidate, flags=re.IGNORECASE)
            candidate = re.split(
                r"\bfor\s+(?:\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*"
                r"(?:people|persons?|guests|travelers|pax)\b",
                candidate, maxsplit=1, flags=re.IGNORECASE,
            )[0]
            candidate = re.split(
                r"\b(?:today|tomorrow|day after tomorrow|next available|morning|afternoon|evening|"
                r"on\s+202\d-\d{2}-\d{2}|at\s+\d{1,2}:\d{2})\b",
                candidate, maxsplit=1, flags=re.IGNORECASE,
            )[0].strip(" .,!?;:")
            if len(candidate) >= 3 and candidate.lower() not in {"a", "the", "tour", "trip", "me"}:
                destination_kw = candidate

    # Extract guests
    guests = req_guests
    if guests is None:
        guest_count_match = re.search(
            r"\b(?:for\s+)?(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*"
            r"(?:people|persons?|guests|travelers|pax)\b",
            objective, re.IGNORECASE,
        )
        if guest_count_match:
            guest_value = guest_count_match.group(1).lower()
            guests = int(guest_value) if guest_value.isdigit() else NUMBER_WORDS.get(guest_value, 1)

    # Extract date using enhanced natural-language parser
    explicit_date_str = None
    if req_date:
        explicit_date_str = req_date.strftime("%Y-%m-%d")
    else:
        explicit_date_str = _parse_natural_date(objective)

    # Extract time slot using enhanced parser
    explicit_slot = req_slot or _parse_time_slot(objective)

    return {
        "destination_keyword": destination_kw,
        "guests": guests,
        "explicit_date": explicit_date_str,
        "explicit_slot": explicit_slot,
    }


# ============================================================================
# LangGraph Workflow Nodes
# ============================================================================

async def interpret_request_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 1: Sanitize input & analyze booking intent."""
    state["completed_steps"].append("Step 1: Sanitize input & analyze booking intent")

    if not state["objective"].strip() or len(state["objective"]) > 2000:
        state["validation_results"]["objective_valid"] = False
        state["status"] = "Failed"
        state["error_message"] = "Please provide a booking request between 1 and 2000 characters."
        return state

    state["validation_results"]["objective_valid"] = True

    conversation_response = classify_conversation(state["objective"])
    if conversation_response:
        state["status"] = "Failed"
        state["error_message"] = conversation_response
        state["validation_results"]["booking_request"] = False
        return state

    # Guardrail: prompt injection / untrusted bypass
    if detect_prompt_injection(state["objective"]):
        state["validation_results"]["prompt_injection_safe"] = False
        state["status"] = "Failed"
        state["approval_status"] = "REJECTED_SECURITY"
        state["error_message"] = (
            "Untrusted prompt or security bypass pattern detected. "
            "Direct confirmation without validation or human approval is strictly prohibited."
        )
        return state

    state["validation_results"]["prompt_injection_safe"] = True

    # Validate traveler exists via backend tool
    traveler_info = await backend_client.check_traveler(state["traveler_id"])
    if not traveler_info.get("exists", False):
        state["validation_results"]["traveler_exists"] = False
        state["status"] = "Failed"
        state["error_message"] = "We couldn't verify your account. Please sign out and sign back in, then try again."
        return state

    state["validation_results"]["traveler_exists"] = True
    if not state.get("traveler_name") and traveler_info.get("fullName"):
        state["traveler_name"] = traveler_info["fullName"]
    if not state.get("traveler_email") and traveler_info.get("email"):
        state["traveler_email"] = traveler_info["email"]

    # Parse intent
    req_date = None
    if state.get("preferred_date"):
        try:
            req_date = datetime.fromisoformat(state["preferred_date"].replace("Z", "+00:00"))
        except Exception:
            pass

    intent = parse_intent_from_objective(
        objective=state["objective"],
        req_destination_id=state.get("preferred_schedule_id"),
        req_date=req_date,
        req_slot=state.get("preferred_time_slot"),
        req_guests=state.get("requested_guests"),
    )

    state["destination_query"] = intent["destination_keyword"]
    state["explicit_date"] = intent["explicit_date"]
    state["explicit_slot"] = intent["explicit_slot"]
    state["guests"] = intent["guests"] or 0

    # Only destination and guests are required — date and slot are now optional.
    # When date/slot are missing the agent will search the schedule and
    # show available upcoming options with capacity info.
    missing_details = []
    if not state["destination_query"] and not state.get("preferred_schedule_id"):
        missing_details.append("the tour or destination")
    if intent["guests"] is None:
        missing_details.append("how many people are traveling")

    if missing_details:
        state["validation_results"]["needs_more_info"] = True
        state["status"] = "Failed"
        state["approval_status"] = "NEEDS_INFO"
        state["error_message"] = (
            "I can help book this, but I still need "
            + " and ".join(missing_details)
            + ". Please add those details and try again."
        )
        return state

    state["validation_results"]["needs_more_info"] = False
    return state


async def create_structured_plan_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 2: Initialize deterministic multi-step plan."""
    state["plan"] = [
        "Step 1: Sanitize input & analyze booking intent",
        "Step 2: Query available tour schedules matching criteria",
        "Step 3: Select schedule and verify time slot availability",
        "Step 4: Check real-time slot capacity via backend tool",
        "Step 5: Check for conflicting bookings for traveler",
        "Step 6: Calculate pricing summary & applicable discounts",
        "Step 7: Execute deterministic validation rules",
        "Step 8: Construct proposal & pause at PENDING_APPROVAL"
    ]
    return state


async def find_available_schedules_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 2: Query available tour schedules matching criteria."""
    state["completed_steps"].append("Step 2: Query available tour schedules matching criteria")

    schedules = await backend_client.get_available_booking_schedules(
        destination_query=state.get("destination_query")
    )

    state["tool_results"]["get_available_booking_schedules"] = {
        "query": state.get("destination_query"),
        "matchedCount": len(schedules),
        "schedules": [
            {
                "id": s.get("id"),
                "destinationTitle": s.get("destinationTitle"),
                "location": s.get("location"),
                "pricePerPerson": s.get("pricePerPerson"),
            }
            for s in schedules
        ],
    }

    selected = None
    pref_id = state.get("preferred_schedule_id")
    if pref_id:
        selected = next((s for s in schedules if str(s.get("id")).lower() == str(pref_id).lower()), None)
        if not selected:
            selected = await backend_client.get_booking_schedule_details(str(pref_id))
    elif schedules:
        selected = schedules[0]

    if not selected:
        state["validation_results"]["schedule_exists"] = False
        state["validation_results"]["schedule_is_bookable"] = False
        state["status"] = "Failed"
        # User-friendly response as strictly required
        state["error_message"] = (
            "Thank you for your request. I couldn't find a matching tour or visiting option "
            "in our currently available schedules. Please try one of the available tours."
        )
        return state

    state["validation_results"]["schedule_exists"] = True
    state["validation_results"]["schedule_is_bookable"] = True
    state["selected_schedule"] = selected
    return state


def _slot_to_minutes(slot: str) -> int:
    """Convert '09:00 AM' style string to minutes since midnight for sorting."""
    m = re.match(r"(\d{1,2}):(\d{2})\s*(AM|PM)", slot.strip(), re.IGNORECASE)
    if not m:
        return 0
    h, mn, period = int(m.group(1)), int(m.group(2)), m.group(3).upper()
    if period == "PM" and h != 12:
        h += 12
    if period == "AM" and h == 12:
        h = 0
    return h * 60 + mn


def _build_availability_message(
    schedule: Dict[str, Any],
    guests: int,
    booked_slots_map: Dict[str, int],
    max_capacity: int,
) -> str:
    """Build a human-readable message listing upcoming dates and slot availability."""
    now = now_sl()
    today_str = now.date().strftime("%Y-%m-%d")
    now_minutes = now.hour * 60 + now.minute

    available_dates = sorted(set(d[:10] for d in schedule.get("availableDates", [])))
    time_slots = sorted(schedule.get("availableTimeSlots", []), key=_slot_to_minutes)

    # Collect per-date slot info, only future slots
    lines = []
    for date_str in available_dates:
        if date_str < today_str:
            continue
        slot_lines = []
        for slot in time_slots:
            slot_min = _slot_to_minutes(slot)
            # Skip past slots for today
            if date_str == today_str and slot_min <= now_minutes:
                continue
            key = f"{date_str}_{slot}"
            booked = booked_slots_map.get(key, 0)
            remaining = max_capacity - booked
            if remaining <= 0:
                slot_lines.append(f"  • {slot} — FULL")
            elif remaining < guests:
                slot_lines.append(f"  • {slot} — Only {remaining} seat(s) left (need {guests})")
            else:
                slot_lines.append(f"  • {slot} — {remaining} seat(s) available ✓")
        if slot_lines:
            try:
                d_obj = datetime.strptime(date_str, "%Y-%m-%d")
                day_label = d_obj.strftime("%A, %d %B %Y")  # e.g. Tuesday, 30 September 2026
            except ValueError:
                day_label = date_str
            lines.append(f"📅 {day_label}")
            lines.extend(slot_lines)

    if not lines:
        return (
            f"I found the **{schedule.get('destinationTitle', 'tour')}** at "
            f"{schedule.get('location', 'the selected location')}, but there are no upcoming "
            f"available slots right now. Please check back later."
        )

    msg = (
        f"I found **{schedule.get('destinationTitle', 'your requested tour')}** "
        f"at {schedule.get('location', '')}!\n\n"
        f"Here are the upcoming available dates and time slots for **{guests} guest(s)**:\n\n"
    )
    msg += "\n".join(lines)
    msg += (
        "\n\nTo book, just tell me which date and time slot you prefer. "
        "For example: *'Book on 2026-10-05 at 09:00 AM'"
    )
    return msg


async def check_capacity_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 3 & 4: Resolve date/slot and check real-time capacity."""
    state["completed_steps"].append("Step 3: Select schedule and verify time slot availability")
    schedule = state["selected_schedule"]
    available_dates = schedule.get("availableDates", [])
    available_slots = schedule.get("availableTimeSlots", [])
    booked_slots_map = schedule.get("bookedSlotsMap", {})
    max_capacity = schedule.get("maxCapacityPerSlot", 8)
    guests = state.get("guests", 1)
    state["guests"] = guests
    now_sl_date = now_sl().date()
    today_str = now_sl_date.strftime("%Y-%m-%d")
    now_minutes = now_sl().hour * 60 + now_sl().minute

    # Validate guest count
    state["validation_results"]["traveler_count_valid"] = (guests > 0)
    if not state["validation_results"]["traveler_count_valid"]:
        state["status"] = "Failed"
        state["error_message"] = "Traveler count must be greater than zero."
        return state

    # --- Case A: No date given → Show all upcoming availability ---
    target_date_str = state.get("explicit_date")
    if not target_date_str:
        avail_msg = _build_availability_message(schedule, guests, booked_slots_map, max_capacity)
        state["validation_results"]["date_is_valid"] = False
        state["status"] = "Failed"
        state["approval_status"] = "NEEDS_DATE"
        state["error_message"] = avail_msg
        return state

    # --- Case B: Date given → validate it ---
    try:
        req_d = datetime.strptime(target_date_str, "%Y-%m-%d").date()
    except ValueError:
        req_d = now_sl_date

    date_offered = any(d[:10] == target_date_str for d in available_dates)
    if not date_offered or req_d < now_sl_date:
        # Show availability instead of a bare error
        avail_msg = _build_availability_message(schedule, guests, booked_slots_map, max_capacity)
        state["validation_results"]["date_is_valid"] = False
        state["status"] = "Failed"
        state["error_message"] = (
            f"The date **{target_date_str}** is not available for this tour.\n\n"
            + avail_msg
        )
        return state

    state["validation_results"]["date_is_valid"] = True
    state["target_date"] = target_date_str

    # --- Resolve time slot ---
    explicit_slot = state.get("explicit_slot")
    # Find closest matching slot (case-insensitive)
    matched_slot = next(
        (s for s in available_slots if s.strip().upper() == (explicit_slot or "").strip().upper()),
        None
    )

    if not explicit_slot:
        # No slot given: show available slots for that date with capacity
        slot_info_lines = []
        for slot in sorted(available_slots, key=_slot_to_minutes):
            slot_min = _slot_to_minutes(slot)
            if target_date_str == today_str and slot_min <= now_minutes:
                continue
            key = f"{target_date_str}_{slot}"
            booked = booked_slots_map.get(key, 0)
            remaining = max_capacity - booked
            if remaining <= 0:
                slot_info_lines.append(f"  • {slot} — FULL")
            elif remaining < guests:
                slot_info_lines.append(f"  • {slot} — Only {remaining} seat(s) left (need {guests})")
            else:
                slot_info_lines.append(f"  • {slot} — {remaining} seat(s) available ✓")

        try:
            d_obj = datetime.strptime(target_date_str, "%Y-%m-%d")
            day_label = d_obj.strftime("%A, %d %B %Y")
        except ValueError:
            day_label = target_date_str

        if slot_info_lines:
            slot_text = "\n".join(slot_info_lines)
            msg = (
                f"Great choice! For **{schedule.get('destinationTitle','the tour')}** "
                f"on **{day_label}**, here are the available time slots for {guests} guest(s):\n\n"
                f"{slot_text}\n\n"
                f"Which time slot would you like? Just reply with the slot, e.g. *'at 09:00 AM'*."
            )
        else:
            msg = (
                f"There are no upcoming slots available on {day_label}. "
                "Please choose another date."
            )
        state["validation_results"]["date_is_valid"] = False
        state["status"] = "Failed"
        state["approval_status"] = "NEEDS_SLOT"
        state["error_message"] = msg
        return state

    if not matched_slot:
        # Slot given but not found in schedule — suggest available ones
        slot_list = ", ".join(available_slots) if available_slots else "none listed"
        state["validation_results"]["date_is_valid"] = False
        state["status"] = "Failed"
        state["error_message"] = (
            f"The time slot **{explicit_slot}** is not available for this tour. "
            f"Available slots are: {slot_list}."
        )
        return state

    state["target_slot"] = matched_slot

    # --- Step 4: Check real-time capacity for the chosen slot ---
    state["completed_steps"].append("Step 4: Check real-time slot capacity via backend tool")
    capacity_res = await backend_client.check_booking_capacity(
        schedule_id=str(schedule["id"]),
        date=target_date_str,
        time_slot=matched_slot,
        requested_guests=guests,
    )
    state["tool_results"]["check_booking_capacity"] = capacity_res
    is_available = capacity_res.get("isAvailable", False)
    state["validation_results"]["capacity_sufficient"] = is_available

    if not is_available:
        # Find other slots on the same date that still have room
        alt_slots = []
        for slot in sorted(available_slots, key=_slot_to_minutes):
            if slot.strip().upper() == matched_slot.strip().upper():
                continue
            key = f"{target_date_str}_{slot}"
            booked = booked_slots_map.get(key, 0)
            remaining = max_capacity - booked
            if remaining >= guests:
                alt_slots.append(f"{slot} ({remaining} seats left)")

        if alt_slots:
            alt_text = ", ".join(alt_slots)
            state["error_message"] = (
                f"The **{matched_slot}** slot on {target_date_str} is fully booked or "
                f"doesn't have enough seats for {guests} guest(s). "
                f"Other available slots on the same day: {alt_text}."
            )
        else:
            state["error_message"] = (
                f"The **{matched_slot}** slot on {target_date_str} is fully booked and "
                f"there are no other slots with enough capacity on that day. "
                "Please choose a different date."
            )
        state["status"] = "Failed"
        return state

    return state


async def check_conflicts_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 5: Check for conflicting bookings for traveler."""
    state["completed_steps"].append("Step 5: Check for conflicting bookings for traveler")
    traveler_bookings = await backend_client.get_traveler_bookings(state["traveler_id"])

    target_date = state["target_date"]
    target_slot = state["target_slot"]

    has_conflict = False
    for b in traveler_bookings:
        if b.get("status") != "Cancelled":
            b_date = (b.get("bookingDate") or "")[:10]
            b_slot = b.get("timeSlot") or ""
            if b_date == target_date and b_slot.lower() == target_slot.lower():
                has_conflict = True
                break

    state["tool_results"]["get_traveler_bookings"] = {
        "existingBookingsCount": len(traveler_bookings),
        "conflictFound": has_conflict,
    }
    state["validation_results"]["no_conflicting_booking"] = not has_conflict

    if has_conflict:
        state["status"] = "Failed"
        # Polite response as required
        state["error_message"] = (
            "I found a conflict with an existing booking, so I couldn't safely continue with this request."
        )
        return state

    return state


async def create_booking_proposal_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 6: Calculate pricing summary & applicable discounts."""
    state["completed_steps"].append("Step 6: Calculate pricing summary & applicable discounts")
    schedule = state["selected_schedule"]
    guests = state["guests"]

    summary = await backend_client.calculate_booking_summary(
        schedule_id=str(schedule["id"]),
        guests=guests,
    )

    if not summary:
        state["status"] = "Failed"
        state["error_message"] = (
            "We were unable to calculate the pricing for this tour at the moment. "
            "Please try again shortly or contact our support team."
        )
        return state

    state["tool_results"]["calculate_booking_summary"] = summary
    state["pricing_summary"] = summary

    # Construct ProposedBooking
    booking_date_iso = f"{state['target_date']}T00:00:00Z"
    proposed = {
        "ScheduleId": schedule["id"],
        "DestinationTitle": schedule.get("destinationTitle", "Tour"),
        "Location": schedule.get("location", ""),
        "BookingDate": booking_date_iso,
        "TimeSlot": state["target_slot"],
        "Guests": guests,
        "PricePerPerson": float(summary.get("pricePerPerson", 0)),
        "BasePrice": float(summary.get("basePrice", 0)),
        "ServiceFee": float(summary.get("serviceFee", 0)),
        "DiscountAmount": float(summary.get("discountAmount", 0)),
        "TotalAmount": float(summary.get("totalAmount", 0)),
        "PaymentMethod": "SampleCard",
    }
    state["proposed_booking"] = proposed
    return state


async def deterministic_validation_node(state: BookingWorkflowState) -> BookingWorkflowState:
    """Step 7 & 8: Execute deterministic validation rules and pause at PENDING_APPROVAL."""
    state["completed_steps"].append("Step 7: Execute deterministic validation rules")

    # Code-based deterministic verification:
    validations = state["validation_results"]
    all_rules_passed = (
        validations.get("traveler_exists", False)
        and validations.get("schedule_exists", False)
        and validations.get("schedule_is_bookable", False)
        and validations.get("date_is_valid", False)
        and validations.get("traveler_count_valid", False)
        and validations.get("capacity_sufficient", False)
        and validations.get("no_conflicting_booking", False)
        and state.get("proposed_booking") is not None
    )

    validations["approval_required_before_creation"] = True

    if not all_rules_passed:
        state["status"] = "Failed"
        if not state.get("error_message"):
            state["error_message"] = "Deterministic validation failed: booking parameters do not satisfy safety criteria."
        return state

    state["completed_steps"].append("Step 8: Construct proposal & pause at PENDING_APPROVAL")
    state["status"] = "PendingApproval"
    state["approval_status"] = "PENDING"
    state["updated_at"] = datetime.utcnow().isoformat()
    return state


# ============================================================================
# Router Functions for Conditional Edges
# ============================================================================

def continue_if_healthy(state: BookingWorkflowState) -> str:
    if state.get("status") == "Failed":
        return END
    return "next"


# ============================================================================
# Graph Construction
# ============================================================================

workflow_graph = StateGraph(BookingWorkflowState)

workflow_graph.add_node("interpret_request", interpret_request_node)
workflow_graph.add_node("create_plan", create_structured_plan_node)
workflow_graph.add_node("find_schedules", find_available_schedules_node)
workflow_graph.add_node("check_capacity", check_capacity_node)
workflow_graph.add_node("check_conflicts", check_conflicts_node)
workflow_graph.add_node("create_proposal", create_booking_proposal_node)
workflow_graph.add_node("deterministic_validation", deterministic_validation_node)

workflow_graph.set_entry_point("interpret_request")

workflow_graph.add_conditional_edges(
    "interpret_request",
    continue_if_healthy,
    {"next": "create_plan", END: END},
)
workflow_graph.add_edge("create_plan", "find_schedules")
workflow_graph.add_conditional_edges(
    "find_schedules",
    continue_if_healthy,
    {"next": "check_capacity", END: END},
)
workflow_graph.add_conditional_edges(
    "check_capacity",
    continue_if_healthy,
    {"next": "check_conflicts", END: END},
)
workflow_graph.add_conditional_edges(
    "check_conflicts",
    continue_if_healthy,
    {"next": "create_proposal", END: END},
)
workflow_graph.add_conditional_edges(
    "create_proposal",
    continue_if_healthy,
    {"next": "deterministic_validation", END: END},
)
workflow_graph.add_edge("deterministic_validation", END)

app_graph = workflow_graph.compile()


# ============================================================================
# Public API Runners
# ============================================================================

async def run_smart_booking_workflow(request: StartAgentBookingRequest) -> AgentWorkflowResponse:
    """Execute the multi-step LangGraph booking agent up to PENDING_APPROVAL."""
    now_iso = datetime.utcnow().isoformat()
    workflow_id = str(uuid.uuid4())

    initial_state: BookingWorkflowState = {
        "workflow_id": workflow_id,
        "traveler_id": str(request.traveler_id),
        "traveler_name": request.traveler_name or "",
        "traveler_email": request.traveler_email or "",
        "objective": request.objective,
        "preferred_schedule_id": str(request.preferred_schedule_id) if request.preferred_schedule_id else None,
        "preferred_date": request.preferred_date.isoformat() if request.preferred_date else None,
        "preferred_time_slot": request.preferred_time_slot,
        "requested_guests": request.guests,
        "status": "Running",
        "plan": [],
        "completed_steps": [],
        "tool_results": {},
        "validation_results": {},
        "destination_query": None,
        "explicit_date": None,
        "explicit_slot": None,
        "guests": request.guests if request.guests is not None else 1,
        "selected_schedule": None,
        "target_date": None,
        "target_slot": None,
        "pricing_summary": None,
        "proposed_booking": None,
        "approval_status": "PENDING",
        "approved_by": None,
        "approver_role": None,
        "approver_notes": None,
        "approved_at": None,
        "created_booking_id": None,
        "booking_reference": None,
        "error_message": None,
        "created_at": now_iso,
        "updated_at": now_iso,
    }

    try:
        final_state = await app_graph.ainvoke(initial_state)
    except Exception as ex:
        # Safe failure handler without exposing stack trace
        final_state = initial_state
        final_state["status"] = "Failed"
        final_state["error_message"] = (
            "I'm sorry, I can currently help with available tours, schedules and booking-related requests. "
            "I can't safely handle that request."
        )

    # Convert proposed booking to ProposedBookingDto
    proposed_dto = None
    if final_state.get("proposed_booking"):
        pb = final_state["proposed_booking"]
        proposed_dto = ProposedBookingDto(
            schedule_id=uuid.UUID(str(pb["ScheduleId"])),
            destination_title=pb["DestinationTitle"],
            location=pb["Location"],
            booking_date=datetime.fromisoformat(pb["BookingDate"].replace("Z", "+00:00")),
            time_slot=pb["TimeSlot"],
            guests=pb["Guests"],
            price_per_person=pb["PricePerPerson"],
            base_price=pb["BasePrice"],
            service_fee=pb["ServiceFee"],
            discount_amount=pb["DiscountAmount"],
            total_amount=pb["TotalAmount"],
            payment_method=pb["PaymentMethod"],
        )

    return AgentWorkflowResponse(
        id=uuid.UUID(final_state["workflow_id"]),
        traveler_id=uuid.UUID(final_state["traveler_id"]),
        traveler_name=final_state["traveler_name"],
        traveler_email=final_state["traveler_email"],
        objective=final_state["objective"],
        status=final_state["status"],
        plan=final_state["plan"],
        completed_steps=final_state["completed_steps"],
        tool_results=final_state["tool_results"],
        validation_results=final_state["validation_results"],
        proposed_booking=proposed_dto,
        created_booking_id=uuid.UUID(final_state["created_booking_id"]) if final_state.get("created_booking_id") else None,
        booking_reference=final_state.get("booking_reference"),
        approval_status=final_state["approval_status"],
        approved_by=final_state.get("approved_by"),
        approver_role=final_state.get("approver_role"),
        approver_notes=final_state.get("approver_notes"),
        approved_at=datetime.fromisoformat(final_state["approved_at"]) if final_state.get("approved_at") else None,
        error_message=final_state.get("error_message"),
        created_at=datetime.fromisoformat(final_state["created_at"]),
        updated_at=datetime.fromisoformat(final_state["updated_at"]),
    )


# ============================================================================
# Post-Approval Execution Flow (Executed only after Authorized Approval)
# ============================================================================

async def execute_approved_booking(
    workflow_data: Dict[str, Any],
    approver_role: str,
    approver_user_id: Optional[str] = None,
    approver_notes: Optional[str] = None,
) -> Dict[str, Any]:
    """Execute the high-impact booking creation tool after authorized Admin/Operator approval."""
    # Deterministic verification of approver role
    role_normalized = (approver_role or "").strip().lower()
    if role_normalized not in ["admin", "operator"]:
        return {
            "success": False,
            "error": "Unauthorized: Only users with 'Admin' or 'Operator' role can approve agent workflows.",
        }

    workflow_id = workflow_data.get("workflow_id") or workflow_data.get("WorkflowId")
    if not workflow_id:
        return {"success": False, "error": "A persisted workflow ID is required."}

    persisted = await backend_client.get_agent_workflow(str(workflow_id))
    if not persisted or str(persisted.get("status", "")).lower() != "pendingapproval":
        return {"success": False, "error": "The workflow is not available for approved execution."}

    persisted_objective = persisted.get("objective") or persisted.get("Objective") or ""
    validation_results = persisted.get("validationResults") or persisted.get("ValidationResults") or {}
    if detect_prompt_injection(persisted_objective) or validation_results.get("prompt_injection_safe") is False:
        return {"success": False, "error": "This proposal contains an unsafe prompt and cannot be approved."}

    persisted_proposed = persisted.get("proposedBooking") or persisted.get("ProposedBooking")
    if not persisted_proposed:
        return {"success": False, "error": "No persisted proposed booking found."}

    workflow_data = {
        "workflow_id": persisted.get("id") or persisted.get("Id"),
        "traveler_id": persisted.get("travelerId") or persisted.get("TravelerId"),
        "traveler_name": persisted.get("travelerName") or persisted.get("TravelerName"),
        "traveler_email": persisted.get("travelerEmail") or persisted.get("TravelerEmail"),
        "proposed_booking": persisted_proposed,
    }

    proposed = workflow_data.get("proposed_booking") or workflow_data.get("ProposedBooking")
    if not proposed:
        return {"success": False, "error": "No proposed booking found in workflow state."}

    # Deterministic creation payload
    create_payload = {
        "ScheduleId": proposed.get("schedule_id") or proposed.get("ScheduleId"),
        "TravelerId": workflow_data.get("traveler_id") or workflow_data.get("TravelerId"),
        "TravelerName": workflow_data.get("traveler_name") or workflow_data.get("TravelerName"),
        "TravelerEmail": workflow_data.get("traveler_email") or workflow_data.get("TravelerEmail"),
        "BookingDate": proposed.get("booking_date") or proposed.get("BookingDate"),
        "TimeSlot": proposed.get("time_slot") or proposed.get("TimeSlot"),
        "Guests": proposed.get("guests") or proposed.get("Guests"),
        "SpecialRequirements": f"Booked via Smart Booking Agent (Approved by {approver_user_id or 'Admin'} - {approver_role})",
        "PaymentMethod": proposed.get("payment_method") or proposed.get("PaymentMethod") or "SampleCard",
    }

    # Tool 6: create_booking
    created_booking, error = await backend_client.create_booking(create_payload)
    if error or not created_booking:
        return {
            "success": False,
            "error": f"Booking execution failed after approval: {error or 'Unknown error'}",
        }

    booking_id = created_booking.get("id")

    # Tool 7: verify booking status
    verified_booking = await backend_client.get_booking_status(str(booking_id))
    booking_ref = (verified_booking or created_booking).get("bookingReference", "")

    return {
        "success": True,
        "created_booking_id": booking_id,
        "booking_reference": booking_ref,
        "completed_step_approval": f"Step 9: Approved by {approver_role} ({approver_user_id or 'Admin'})",
        "completed_step_execution": f"Step 10: Created booking {booking_ref} and verified escrow status",
    }
