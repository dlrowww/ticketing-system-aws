# Seed Data Validation Document

**Purpose:** Verify that UI displays match the seeded data from demo-data.json  
**Last Updated:** January 13, 2026  
**Total Tickets:** 25  
**Users:** 8 (1 Admin, 3 TeamLeaders, 3 Support, 2 Employees)  
**Categories:** 3 (IT, Logistics, Administration)

---

## Quick Reference Table

| User | Role | Category | My Workload | My Requests | Category View | Unassigned in Category |
|------|------|----------|-------------|-------------|---------------|------------------------|
| michael.johnson@ironpack.pl | TeamLeader | IT | 3 | 0 | 10 | 2 |
| david.smith@ironpack.pl | Support | IT | 6 | 0 | 10 | 2 |
| james.wilson@ironpack.pl | TeamLeader | Logistics | 4 | 0 | 8 | 2 |
| sarah.mitchell@ironpack.pl | Support | Logistics | 3 | 0 | 8 | 2 |
| emily.davis@ironpack.pl | TeamLeader | Administration | 2 | 0 | 7 | 2 |
| robert.brown@ironpack.pl | Support | Administration | 3 | 0 | 7 | 2 |
| lisa.anderson@ironpack.pl | Employee | - | 0 | 19 | - | - |
| tom.harris@ironpack.pl | Employee | - | 0 | 6 | - | - |
| admin@ironpack.pl | Admin | - | - | - | 25 (All) | - |

**Notes:**
- **My Workload**: Tickets assigned to this user
- **My Requests**: Tickets created by this user (Employee role only sees this)
- **Category View**: All tickets in user's category (TeamLeader/Support)
- **Unassigned in Category**: Tickets with no assignee in user's category (Support sees these in "Unassigned Pool")

---

## Overall Statistics

### Tickets by Status
- **New**: 5 tickets
- **Open**: 4 tickets
- **InProcess**: 6 tickets
- **Resolved**: 7 tickets
- **Cancelled**: 1 ticket
- **Postponed**: 1 ticket
- **Returned**: 1 ticket

### Tickets by Priority
- **Low**: 6 tickets
- **Medium**: 10 tickets
- **High**: 6 tickets
- **Critical**: 3 tickets

### Tickets by Category
- **IT**: 10 tickets
- **Logistics**: 8 tickets
- **Administration**: 7 tickets

### Special Counts
- **With Comments**: 17 tickets
- **With Attachments**: 7 tickets

---

## Dashboard Data

### Admin Dashboard (All Tickets)
- **Pending** (New + Open): 9 tickets
- **In Progress**: 6 tickets
- **Resolved**: 7 tickets

### Active Tickets by Category (New + Open + InProcess)
- **IT**: 7 tickets (New: 2, Open: 2, InProcess: 3)
- **Logistics**: 6 tickets (New: 2, Open: 1, InProcess: 2, Postpone: 1)
- **Administration**: 4 tickets (New: 1, Open: 1, InProcess: 1, Returned: 1)
- **Total Active**: 17 tickets

### Pending Tickets by Category (New + Open)
- **IT**: 4 tickets (New: 2, Open: 2)
- **Logistics**: 3 tickets (New: 2, Open: 1)
- **Administration**: 2 tickets (New: 1, Open: 1)
- **Total Pending**: 9 tickets

### TeamLeader Dashboards - Active Tickets by Priority

#### IT TeamLeader (michael.johnson@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 7 tickets
- **Low**: 1 ticket (#6 New)
- **Medium**: 3 tickets (#3 InProcess, #4 Open, #8 Open)
- **High**: 2 tickets (#5 InProcess, #10 New)
- **Critical**: 1 ticket (#7 InProcess)

**Pending Tickets** (New + Open): 4 tickets
- Low: 1, Medium: 2, High: 1

#### Logistics TeamLeader (james.wilson@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 5 tickets
- **Low**: 1 ticket (#14 New)
- **Medium**: 2 tickets (#12 InProcess, #18 New)
- **High**: 2 tickets (#13 Open, #16 InProcess)

**Pending Tickets** (New + Open): 3 tickets
- Low: 1, Medium: 1, High: 1

#### Administration TeamLeader (emily.davis@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 3 tickets
- **Low**: 1 ticket (#23 New)
- **Medium**: 2 tickets (#20 Open, #22 InProcess)

**Pending Tickets** (New + Open): 2 tickets
- Low: 1, Medium: 1

---

## Category Breakdown (for TeamLeaders & Support)

### IT Category (10 tickets total)

**Unassigned Tickets: 2**
- Ticket #6: "Printer not responding (Floor 2)" - Priority: Low, Status: New, Creator: tom.harris
- Ticket #10: "Backup job failing - Critical data at risk" - Priority: High, Status: New, Creator: lisa.anderson

**By Status:**
- New: 2
- Open: 2
- InProcess: 3
- Resolved: 2
- Cancelled: 1

**By Priority:**
- Low: 2
- Medium: 3
- High: 3
- Critical: 2

**All IT Tickets:**
1. Production server experiencing high CPU usage (Critical, Resolved, assigned to david.smith)
2. Laptop keyboard not responding (High, Resolved, assigned to michael.johnson)
3. Wi-Fi connection drops frequently in Building A (Medium, InProcess, assigned to david.smith)
4. Email client crashes on startup (Medium, Open, assigned to david.smith)
5. VPN connection timeout errors (High, InProcess, assigned to michael.johnson)
6. Printer not responding (Floor 2) (Low, New, **UNASSIGNED**)
7. Database queries running slow on production (Critical, InProcess, assigned to david.smith)
8. Antivirus update failed on multiple computers (Medium, Open, assigned to david.smith)
9. File server access denied error (Low, Cancelled, assigned to michael.johnson)
10. Backup job failing - Critical data at risk (High, New, **UNASSIGNED**)

---

### Logistics Category (8 tickets total)

**Unassigned Tickets: 2**
- Ticket #14: "Loading dock light broken" - Priority: Low, Status: New, Creator: tom.harris
- Ticket #18: "Pallet jack damaged - replacement needed" - Priority: Medium, Status: New, Creator: lisa.anderson

**By Status:**
- New: 2
- Open: 1
- InProcess: 3
- Resolved: 2
- Postponed: 1

**By Priority:**
- Low: 1
- Medium: 4
- High: 3

**All Logistics Tickets:**
11. Warehouse shipment delayed - customer impact (High, Resolved, assigned to james.wilson)
12. Forklift maintenance required (Medium, InProcess, assigned to sarah.mitchell)
13. Inventory discrepancy in Zone C (High, Open, assigned to james.wilson)
14. Loading dock light broken (Low, New, **UNASSIGNED**)
15. Missing delivery paperwork (Medium, Resolved, assigned to sarah.mitchell)
16. Packaging materials shortage (High, InProcess, assigned to james.wilson)
17. Delivery truck GPS malfunction (Medium, Postponed, assigned to sarah.mitchell)
18. Pallet jack damaged - replacement needed (Medium, New, **UNASSIGNED**)

---

### Administration Category (7 tickets total)

**Unassigned Tickets: 2**
- Ticket #23: "Parking permit request for new employee" - Priority: Low, Status: New, Creator: lisa.anderson
- Ticket #25: Fire alarm test scheduled - notify all staff (Critical, Returned, assigned to robert.brown) - **Note: Returned status, but has assignee**

**By Status:**
- New: 1
- Open: 1
- InProcess: 1
- Resolved: 3
- Returned: 1

**By Priority:**
- Low: 3
- Medium: 3
- Critical: 1

**All Administration Tickets:**
19. Office supplies order needed (Low, Resolved, assigned to robert.brown)
20. Access card not working at main entrance (Medium, Open, assigned to robert.brown)
21. Conference room booking system error (Low, Resolved, assigned to emily.davis)
22. HVAC temperature too cold in office area (Medium, InProcess, assigned to robert.brown)
23. Parking permit request for new employee (Low, New, **UNASSIGNED**)
24. Employee onboarding checklist incomplete (Medium, Resolved, assigned to emily.davis)
25. Fire alarm test scheduled - notify all staff (Critical, Returned, assigned to robert.brown)

---

## Per-User Views

### Admin (admin@ironpack.pl)

**View:** All Tickets (no filtering)  
**Expected Count:** 25 tickets

Admin sees all tickets regardless of category, status, or assignment.

---

### TeamLeader - IT (michael.johnson@ironpack.pl)

**My Workload:** 3 tickets (assigned to me)
- Ticket #2: Laptop keyboard not responding (High, Resolved)
- Ticket #5: VPN connection timeout errors (High, InProcess)
- Ticket #9: File server access denied error (Low, Cancelled)

**Team Tickets:** See [IT Category Breakdown](#it-category-10-tickets-total) above (10 tickets total)

**Breakdown of My Workload:**
- By Status: InProcess: 1, Resolved: 1, Cancelled: 1
- By Priority: Low: 1, High: 2
- All are IT category

---

### Support - IT (david.smith@ironpack.pl)

**My Workload:** 6 tickets (assigned to me)
- Ticket #1: Production server experiencing high CPU usage (Critical, Resolved)
- Ticket #3: Wi-Fi connection drops frequently in Building A (Medium, InProcess)
- Ticket #4: Email client crashes on startup (Medium, Open)
- Ticket #7: Database queries running slow on production (Critical, InProcess)
- Ticket #8: Antivirus update failed on multiple computers (Medium, Open)

**Unassigned Pool:** See [IT Category Breakdown](#it-category-10-tickets-total) - Unassigned section (2 tickets)

**Breakdown of My Workload:**
- By Status: Open: 2, InProcess: 2, Resolved: 1
- By Priority: Medium: 3, Critical: 2
- All are IT category

---

### TeamLeader - Logistics (james.wilson@ironpack.pl)

**My Workload:** 4 tickets (assigned to me)
- Ticket #11: Warehouse shipment delayed - customer impact (High, Resolved)
- Ticket #13: Inventory discrepancy in Zone C (High, Open)
- Ticket #16: Packaging materials shortage (High, InProcess)

**Team Tickets:** See [Logistics Category Breakdown](#logistics-category-8-tickets-total) above (8 tickets total)

**Breakdown of My Workload:**
- By Status: Open: 1, InProcess: 1, Resolved: 1
- By Priority: High: 3
- All are Logistics category

---

### Support - Logistics (sarah.mitchell@ironpack.pl)

**My Workload:** 3 tickets (assigned to me)
- Ticket #12: Forklift maintenance required (Medium, InProcess)
- Ticket #15: Missing delivery paperwork (Medium, Resolved)
- Ticket #17: Delivery truck GPS malfunction (Medium, Postponed)

**Unassigned Pool:** See [Logistics Category Breakdown](#logistics-category-8-tickets-total) - Unassigned section (2 tickets)

**Breakdown of My Workload:**
- By Status: InProcess: 1, Resolved: 1, Postponed: 1
- By Priority: Medium: 3
- All are Logistics category

---

### TeamLeader - Administration (emily.davis@ironpack.pl)

**My Workload:** 2 tickets (assigned to me)
- Ticket #21: Conference room booking system error (Low, Resolved)
- Ticket #24: Employee onboarding checklist incomplete (Medium, Resolved)

**Team Tickets:** See [Administration Category Breakdown](#administration-category-7-tickets-total) above (7 tickets total)

**Breakdown of My Workload:**
- By Status: Resolved: 2
- By Priority: Low: 1, Medium: 1
- All are Administration category

---

### Support - Administration (robert.brown@ironpack.pl)

**My Workload:** 3 tickets (assigned to me)
- Ticket #19: Office supplies order needed (Low, Resolved)
- Ticket #20: Access card not working at main entrance (Medium, Open)
- Ticket #22: HVAC temperature too cold in office area (Medium, InProcess)
- Ticket #25: Fire alarm test scheduled - notify all staff (Critical, Returned)

**Note:** Ticket #25 is in "Returned" status but still shows in My Workload because it's assigned.

**Unassigned Pool:** See [Administration Category Breakdown](#administration-category-7-tickets-total) - Unassigned section (2 tickets, but one has assignee with Returned status)

**Breakdown of My Workload:**
- By Status: Open: 1, InProcess: 1, Resolved: 1, Returned: 1
- By Priority: Low: 1, Medium: 2, Critical: 1
- All are Administration category

---

### Employee (lisa.anderson@ironpack.pl)

**My Requests:** 18 tickets (created by me - heavy ticket creator)
- Ticket #1: Production server experiencing high CPU usage (Critical, Resolved, IT)
- Ticket #2: Laptop keyboard not responding (High, Resolved, IT)
- Ticket #3: Wi-Fi connection drops frequently in Building A (Medium, InProcess, IT)
- Ticket #4: Email client crashes on startup (Medium, Open, IT)
- Ticket #5: VPN connection timeout errors (High, InProcess, IT)
- Ticket #7: Database queries running slow on production (Critical, InProcess, IT)
- Ticket #9: File server access denied error (Low, Cancelled, IT)
- Ticket #10: Backup job failing - Critical data at risk (High, New, IT)
- Ticket #11: Warehouse shipment delayed - customer impact (High, Resolved, Logistics)
- Ticket #13: Inventory discrepancy in Zone C (High, Open, Logistics)
- Ticket #15: Missing delivery paperwork (Medium, Resolved, Logistics)
- Ticket #16: Packaging materials shortage (High, InProcess, Logistics)
- Ticket #18: Pallet jack damaged - replacement needed (Medium, New, Logistics)
- Ticket #19: Office supplies order needed (Low, Resolved, Administration)
- Ticket #21: Conference room booking system error (Low, Resolved, Administration)
- Ticket #23: Parking permit request for new employee (Low, New, Administration)
- Ticket #24: Employee onboarding checklist incomplete (Medium, Resolved, Administration)
- Ticket #25: Fire alarm test scheduled - notify all staff (Critical, Returned, Administration)

**Note:** Employee role can ONLY see tickets they created. They cannot see other users' tickets.

**Breakdown of My Requests:**
- By Status: New: 3, Open: 2, InProcess: 4, Resolved: 7, Cancelled: 1, Returned: 1
- By Priority: Low: 4, Medium: 6, High: 6, Critical: 3
- By Category: IT: 8, Logistics: 5, Administration: 6

**Resolved Tickets:** #1, #2, #11, #15, #19, #21, #24

---

### Employee (tom.harris@ironpack.pl)

**My Requests:** 7 tickets (created by me)
- Ticket #6: Printer not responding (Floor 2) (Low, New, IT)
- Ticket #8: Antivirus update failed on multiple computers (Medium, Open, IT)
- Ticket #12: Forklift maintenance required (Medium, InProcess, Logistics)
- Ticket #14: Loading dock light broken (Low, New, Logistics)
- Ticket #17: Delivery truck GPS malfunction (Medium, Postponed, Logistics)
- Ticket #20: Access card not working at main entrance (Medium, Open, Administration)
- Ticket #22: HVAC temperature too cold in office area (Medium, InProcess, Administration)

**Note:** Employee role can ONLY see tickets they created.

**Breakdown of My Requests:**
- By Status: New: 2, Open: 2, InProcess: 2, Postponed: 1
- By Priority: Low: 2, Medium: 5
- By Category: IT: 2, Logistics: 3, Administration: 2

---

## Date Range Filters

**Today's Date:** January 13, 2026

### Last 7 Days (since January 6, 2026)
**Expected Count:** 13 tickets

Tickets created within last 7 days (createdDaysAgo <= 7):
- Ticket #3: Wi-Fi connection drops frequently in Building A (5 days ago)
- Ticket #4: Email client crashes on startup (3 days ago)
- Ticket #5: VPN connection timeout errors (7 days ago)
- Ticket #6: Printer not responding (Floor 2) (2 days ago)
- Ticket #8: Antivirus update failed on multiple computers (4 days ago)
- Ticket #10: Backup job failing - Critical data at risk (1 day ago)
- Ticket #12: Forklift maintenance required (6 days ago)
- Ticket #13: Inventory discrepancy in Zone C (4 days ago)
- Ticket #14: Loading dock light broken (3 days ago)
- Ticket #18: Pallet jack damaged - replacement needed (2 days ago)
- Ticket #20: Access card not working at main entrance (5 days ago)
- Ticket #22: HVAC temperature too cold in office area (7 days ago)
- Ticket #23: Parking permit request for new employee (4 days ago)

**Note:** The metadata in demo-data.json shows last7Days: 18, which may use a different calculation method or reference date than our manual count.

### Last 30 Days (since December 14, 2025)
**Expected Count:** 24 tickets

All tickets except:
- Ticket #1: Production server experiencing high CPU usage (45 days ago)
- Ticket #2: Laptop keyboard not responding (30 days ago) - **Exactly 30 days, may be included**
- Ticket #24: Employee onboarding checklist incomplete (30 days ago) - **Exactly 30 days, may be included**

**Tickets in range (createdDaysAgo <= 30):** 23-25 tickets depending on whether 30-day boundary is inclusive.

### Last 90 Days (since October 15, 2025)
**Expected Count:** 25 tickets (all tickets)

All 25 tickets are within the last 90 days.

**Date Range Note:** The seed data uses "createdDaysAgo" from seeding time, so actual dates will shift over time. For demo purposes on January 13, 2026, use the counts above as reference.

---

## Verification Checklist

### During Demo - What to Check:

#### Admin Login (admin@ironpack.pl)
- [ ] Dashboard shows: Total: 25, Pending: 9, In Progress: 6, Resolved: 7
- [ ] Dashboard by Status: New:5, Open:4, InProcess:6, Resolved:7, Cancelled:1, Postponed:1, Returned:1
- [ ] All Tickets view shows 25 tickets
- [ ] Filter by Category: IT=10, Logistics=8, Administration=7
- [ ] Filter by Status: New=5, Open=4, InProcess=6, Resolved=7, Cancelled=1, Postponed=1, Returned=1
- [ ] Filter by Priority: Low=6, Medium=10, High=6, Critical=3
- [ ] Active tickets (New+Open+InProcess): 17 total

#### TeamLeader Login (michael.johnson@ironpack.pl)
- [ ] My Workload shows 3 tickets (#2, #5, #9)
- [ ] Team Tickets (IT) sh
- [ ] Dashboard shows IT category: Pending: 4, Active: 7
- [ ] Dashboard shows Active by Priority: Low:1, Medium:3, High:2, Critical:1ows 10 tickets
- [ ] Can see unassigned IT tickets (#6, #10)
- [ ] Dashboard accessible

#### Support Login (david.smith@ironpack.pl)
- [ ] My Workload shows 6 tickets (#1, #3, #4, #7, #8)
- [ ] Unassigned Pool shows 2 IT tickets (#6, #10)
- [ ] Can see all IT category tickets (10 total)
- [ ] Cannot see Logistics or Administration tickets

#### Employee Login (lisa.anderson@ironpack.pl): 
- [ ] 18 tickets
- [ ] Cannot see tickets created by tom.harris
- [ ] Cannot see "All Tickets" or "Unassigned Pool" navigation items
- [ ] Can filter own tickets by Status/Priority/Category
7
#### Employee Login (tom.harris@ironpack.pl)
- [ ] My Requests shows 7 tickets (all created by tom.harris)
- [ ] Cannot see tickets created by lisa.anderson

---

## Special Test Cases

### Unassigned Tickets
**Total Unassigned:** 6 tickets
- IT: #6 (Low, New), #10 (High, New)
- Logistics: #14 (Low, New), #18 (Medium, New)
- Administration: #23 (Low, New)

**Note:** Ticket #25 (Administration, Critical, Returned) has assignee=robert.brown, so it's NOT unassigned despite Returned status.

### Tickets with Attachments (7 tickets)
- Ticket #1: 2 attachments (1-PNG.png, 2-PDF.pdf)
- Ticket #3: 1 attachment (3-JPG.jpg)
- Ticket #4: 1 attachment (4.zip)
- Ticket #5: 1 attachment (5-PNG.png)
- Ticket #7: 2 attachments (1-PNG.png, 2-PDF.pdf)
- Ticket #12: 1 attachment (3-JPG.jpg)
- Ticket #15: 1 attachment (2-PDF.pdf)

### Tickets with Comments (17 tickets)
All tickets except:
- #6, #10, #14, #16, #18, #20, #23, #25

### Tickets with Internal Comments (9 tickets)
- #1 (1 internal), #3 (1 internal), #5 (1 internal), #7 (1 internal), #9 (1 internal)
- #13 (1 internal), #16 (1 internal), #22 (1 internal), #25 (1 internal)

---

**End of Validation Document**  
**Use this document during demo to verify UI displays against expected seed data.**
