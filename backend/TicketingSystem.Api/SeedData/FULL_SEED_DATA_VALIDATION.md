# Full Seed Data Validation Document

**Purpose:** Verify that UI displays match the seeded data from full-seed-data.json  
**Last Updated:** January 13, 2026  
**Total Tickets:** 49  
**Users:** 8 (1 Admin, 3 TeamLeaders, 3 Support, 2 Employees)  
**Categories:** 3 (IT, Logistics, Administration)

---

## Quick Reference Table

| User | Role | Category | My Workload | My Requests | Category View | Unassigned in Category |
|------|------|----------|-------------|-------------|---------------|------------------------|
| michael.johnson@ironpack.pl | TeamLeader | IT | 7 | 0 | 17 | 1 |
| david.smith@ironpack.pl | Support | IT | 9 | 0 | 17 | 1 |
| james.wilson@ironpack.pl | TeamLeader | Logistics | 8 | 0 | 17 | 2 |
| sarah.mitchell@ironpack.pl | Support | Logistics | 7 | 0 | 17 | 2 |
| emily.davis@ironpack.pl | TeamLeader | Administration | 5 | 0 | 15 | 1 |
| robert.brown@ironpack.pl | Support | Administration | 8 | 0 | 15 | 1 |
| lisa.anderson@ironpack.pl | Employee | - | 0 | 31 | - | - |
| tom.harris@ironpack.pl | Employee | - | 0 | 18 | - | - |
| admin@ironpack.pl | Admin | - | - | - | 49 (All) | - |

**Notes:**
- **My Workload**: Tickets assigned to this user
- **My Requests**: Tickets created by this user (Employee role only sees this)
- **Category View**: All tickets in user's category (TeamLeader/Support)
- **Unassigned in Category**: Tickets with no assignee in user's category (Support sees these in "Unassigned Pool")

---

## Overall Statistics

### Tickets by Status
- **New**: 5 tickets
- **Open**: 9 tickets
- **InProcess**: 14 tickets
- **Resolved**: 16 tickets
- **Cancelled**: 2 tickets
- **Postponed**: 2 tickets
- **Returned**: 1 ticket

### Tickets by Priority
- **Low**: 11 tickets
- **Medium**: 21 tickets
- **High**: 12 tickets
- **Critical**: 5 tickets

### Tickets by Category
- **IT**: 17 tickets
- **Logistics**: 17 tickets
- **Administration**: 15 tickets

### Special Counts
- **With Comments**: 43 tickets
- **With Attachments**: 12 tickets

---

## Dashboard Data

### Admin Dashboard (All Tickets)
- **Pending** (New + Open): 14 tickets
- **In Progress**: 14 tickets
- **Resolved**: 16 tickets

### Active Tickets by Category (New + Open + InProcess)
- **IT**: 9 tickets (New: 1, Open: 3, InProcess: 5)
- **Logistics**: 11 tickets (New: 2, Open: 4, InProcess: 5)
- **Administration**: 8 tickets (New: 1, Open: 3, InProcess: 4)
- **Total Active**: 28 tickets

### Pending Tickets by Category (New + Open)
- **IT**: 4 tickets (New: 1, Open: 3)
- **Logistics**: 6 tickets (New: 2, Open: 4)
- **Administration**: 4 tickets (New: 1, Open: 3)
- **Total Pending**: 14 tickets

### TeamLeader Dashboards - Active Tickets by Priority

#### IT TeamLeader (michael.johnson@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 9 tickets
- **Low**: 1 ticket (#6 New)
- **Medium**: 4 tickets (#4 Open, #8 Open, #29 Open, #32 InProcess)
- **High**: 3 tickets (#5 InProcess, #10 New, #27 InProcess)
- **Critical**: 1 ticket (#7 InProcess)

**Pending Tickets** (New + Open): 4 tickets
- Low: 1, Medium: 2, High: 1

#### Logistics TeamLeader (james.wilson@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 11 tickets
- **Low**: 1 ticket (#37 Open)
- **Medium**: 6 tickets (#12 InProcess, #18 New, #38 InProcess, #40 InProcess, #41 Open, #42 Open)
- **High**: 3 tickets (#13 Open, #16 InProcess, #35 InProcess)
- **Critical**: 1 ticket (#36 InProcess)

**Pending Tickets** (New + Open): 6 tickets
- Low: 1, Medium: 3, High: 1, Critical: 1

#### Administration TeamLeader (emily.davis@ironpack.pl)
**Active Tickets** (New + Open + InProcess): 8 tickets
- **Low**: 2 tickets (#23 New, #45 Open)
- **Medium**: 5 tickets (#20 Open, #22 InProcess, #43 InProcess, #44 Open, #47 InProcess)
- **High**: 1 ticket (#48 InProcess)

**Pending Tickets** (New + Open): 4 tickets
- Low: 2, Medium: 2

---

## Category Breakdown (for TeamLeaders & Support)

### IT Category (17 tickets total)

**Unassigned Tickets: 1**
- Ticket #10: "Backup job failing - Critical data at risk" - Priority: High, Status: New, Creator: lisa.anderson

**By Status:**
- New: 1
- Open: 3
- InProcess: 5
- Resolved: 7
- Cancelled: 1

**By Priority:**
- Low: 2
- Medium: 7
- High: 5
- Critical: 3

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
26. Secondary server backup verification failed (Critical, Resolved, assigned to david.smith)
27. Network switch showing errors on ports 12-24 (High, InProcess, assigned to michael.johnson)
28. Spam filter blocking legitimate emails (Medium, Resolved, assigned to david.smith)
29. Software license renewal approaching (Medium, Open, assigned to michael.johnson)
30. Monitor flickering issue - multiple workstations (Low, InProcess, assigned to david.smith)
31. Mobile device management enrollment failing (High, Resolved, assigned to michael.johnson)
32. Cloud storage quota exceeded (Medium, InProcess, assigned to david.smith)

---

### Logistics Category (17 tickets total)

**Unassigned Tickets: 2**
- Ticket #14: "Loading dock light broken" - Priority: Low, Status: New, Creator: tom.harris
- Ticket #18: "Pallet jack damaged - replacement needed" - Priority: Medium, Status: New, Creator: lisa.anderson

**By Status:**
- New: 2
- Open: 4
- InProcess: 5
- Resolved: 5
- Postponed: 1

**By Priority:**
- Low: 1
- Medium: 8
- High: 5
- Critical: 3

**All Logistics Tickets:**
11. Warehouse shipment delayed - customer impact (High, Resolved, assigned to james.wilson)
12. Forklift maintenance required (Medium, InProcess, assigned to sarah.mitchell)
13. Inventory discrepancy in Zone C (High, Open, assigned to james.wilson)
14. Loading dock light broken (Low, New, **UNASSIGNED**)
15. Missing delivery paperwork (Medium, Resolved, assigned to sarah.mitchell)
16. Packaging materials shortage (High, InProcess, assigned to james.wilson)
17. Delivery truck GPS malfunction (Medium, Postponed, assigned to sarah.mitchell)
18. Pallet jack damaged - replacement needed (Medium, New, **UNASSIGNED**)
33. Inventory scanner batteries draining quickly (Medium, Resolved, assigned to james.wilson)
34. Shipping label printer jamming frequently (High, Open, assigned to sarah.mitchell)
35. Incorrect inventory count after annual audit (High, InProcess, assigned to james.wilson)
36. Loading dock door motor failure (Critical, Resolved, assigned to james.wilson)
37. Delivery route optimization request (Medium, InProcess, assigned to sarah.mitchell)
38. Packing station ergonomics complaint (Low, Open, assigned to james.wilson)
39. Refrigerated storage temperature fluctuation (High, Resolved, assigned to sarah.mitchell)
40. Return processing backlog (Medium, InProcess, assigned to james.wilson)
41. Courier service contract renewal (Medium, Open, assigned to sarah.mitchell)

---

### Administration Category (15 tickets total)

**Unassigned Tickets: 1**
- Ticket #23: "Parking permit request for new employee" - Priority: Low, Status: New, Creator: lisa.anderson

**By Status:**
- New: 1
- Open: 3
- InProcess: 4
- Resolved: 5
- Cancelled: 1
- Postponed: 1

**By Priority:**
- Low: 4
- Medium: 8
- High: 1
- Critical: 2

**All Administration Tickets:**
19. Office supplies order needed (Low, Resolved, assigned to robert.brown)
20. Access card not working at main entrance (Medium, Open, assigned to robert.brown)
21. Conference room booking system error (Low, Resolved, assigned to emily.davis)
22. HVAC temperature too cold in office area (Medium, InProcess, assigned to robert.brown)
23. Parking permit request for new employee (Low, New, **UNASSIGNED**)
24. Employee onboarding checklist incomplete (Medium, Resolved, assigned to emily.davis)
25. Fire alarm test scheduled - notify all staff (Critical, Returned, assigned to robert.brown)
42. Meeting room projector bulb replacement (Medium, Resolved, assigned to robert.brown)
43. Office cleaning schedule conflict (Low, Resolved, assigned to emily.davis)
44. Employee handbook update needed (Medium, InProcess, assigned to emily.davis)
45. Visitor parking validation system down (Low, Postponed, assigned to robert.brown)
46. Company newsletter deadline approaching (Medium, Open, assigned to emily.davis)
47. Building security camera blind spot identified (High, InProcess, assigned to robert.brown)
48. First aid kit expiration - multiple locations (Medium, Resolved, assigned to emily.davis)
49. Break room refrigerator not cooling (Low, Cancelled, assigned to robert.brown)

---

## Per-User Views

### Admin (admin@ironpack.pl)

**View:** All Tickets (no filtering)  
**Expected Count:** 49 tickets

Admin sees all tickets regardless of category, status, or assignment.

---

### TeamLeader - IT (michael.johnson@ironpack.pl)

**My Workload:** 7 tickets (assigned to me)
- Ticket #2: Laptop keyboard not responding (High, Resolved)
- Ticket #5: VPN connection timeout errors (High, InProcess)
- Ticket #9: File server access denied error (Low, Cancelled)
- Ticket #27: Network switch showing errors on ports 12-24 (High, InProcess)
- Ticket #29: Software license renewal approaching (Medium, Open)
- Ticket #31: Mobile device management enrollment failing (High, Resolved)

**Team Tickets:** See [IT Category Breakdown](#it-category-17-tickets-total) above (17 tickets total)

**Breakdown of My Workload:**
- By Status: Open: 1, InProcess: 2, Resolved: 2, Cancelled: 1
- By Priority: Low: 1, Medium: 1, High: 4
- All are IT category

---

### Support - IT (david.smith@ironpack.pl)

**My Workload:** 9 tickets (assigned to me)
- Ticket #1: Production server experiencing high CPU usage (Critical, Resolved)
- Ticket #3: Wi-Fi connection drops frequently in Building A (Medium, InProcess)
- Ticket #4: Email client crashes on startup (Medium, Open)
- Ticket #7: Database queries running slow on production (Critical, InProcess)
- Ticket #8: Antivirus update failed on multiple computers (Medium, Open)
- Ticket #26: Secondary server backup verification failed (Critical, Resolved)
- Ticket #28: Spam filter blocking legitimate emails (Medium, Resolved)
- Ticket #30: Monitor flickering issue - multiple workstations (Low, InProcess)
- Ticket #32: Cloud storage quota exceeded (Medium, InProcess)

**Unassigned Pool:** See [IT Category Breakdown](#it-category-17-tickets-total) - Unassigned section (1 ticket)

**Breakdown of My Workload:**
- By Status: Open: 2, InProcess: 4, Resolved: 3
- By Priority: Low: 1, Medium: 5, Critical: 3
- All are IT category

---

### TeamLeader - Logistics (james.wilson@ironpack.pl)

**My Workload:** 8 tickets (assigned to me)
- Ticket #11: Warehouse shipment delayed - customer impact (High, Resolved)
- Ticket #13: Inventory discrepancy in Zone C (High, Open)
- Ticket #16: Packaging materials shortage (High, InProcess)
- Ticket #33: Inventory scanner batteries draining quickly (Medium, Resolved)
- Ticket #35: Incorrect inventory count after annual audit (High, InProcess)
- Ticket #36: Loading dock door motor failure (Critical, Resolved)
- Ticket #38: Packing station ergonomics complaint (Low, Open)
- Ticket #40: Return processing backlog (Medium, InProcess)

**Team Tickets:** See [Logistics Category Breakdown](#logistics-category-17-tickets-total) above (17 tickets total)

**Breakdown of My Workload:**
- By Status: Open: 2, InProcess: 3, Resolved: 3
- By Priority: Low: 1, Medium: 2, High: 4, Critical: 1
- All are Logistics category

---

### Support - Logistics (sarah.mitchell@ironpack.pl)

**My Workload:** 7 tickets (assigned to me)
- Ticket #12: Forklift maintenance required (Medium, InProcess)
- Ticket #15: Missing delivery paperwork (Medium, Resolved)
- Ticket #17: Delivery truck GPS malfunction (Medium, Postponed)
- Ticket #34: Shipping label printer jamming frequently (High, Open)
- Ticket #37: Delivery route optimization request (Medium, InProcess)
- Ticket #39: Refrigerated storage temperature fluctuation (High, Resolved)
- Ticket #41: Courier service contract renewal (Medium, Open)

**Unassigned Pool:** See [Logistics Category Breakdown](#logistics-category-17-tickets-total) - Unassigned section (2 tickets)

**Breakdown of My Workload:**
- By Status: Open: 2, InProcess: 2, Resolved: 2, Postponed: 1
- By Priority: Medium: 5, High: 2
- All are Logistics category

---

### TeamLeader - Administration (emily.davis@ironpack.pl)

**My Workload:** 5 tickets (assigned to me)
- Ticket #21: Conference room booking system error (Low, Resolved)
- Ticket #24: Employee onboarding checklist incomplete (Medium, Resolved)
- Ticket #43: Office cleaning schedule conflict (Low, Resolved)
- Ticket #44: Employee handbook update needed (Medium, InProcess)
- Ticket #46: Company newsletter deadline approaching (Medium, Open)
- Ticket #48: First aid kit expiration - multiple locations (Medium, Resolved)

**Team Tickets:** See [Administration Category Breakdown](#administration-category-15-tickets-total) above (15 tickets total)

**Breakdown of My Workload:**
- By Status: Open: 1, InProcess: 1, Resolved: 4
- By Priority: Low: 2, Medium: 4
- All are Administration category

---

### Support - Administration (robert.brown@ironpack.pl)

**My Workload:** 8 tickets (assigned to me)
- Ticket #19: Office supplies order needed (Low, Resolved)
- Ticket #20: Access card not working at main entrance (Medium, Open)
- Ticket #22: HVAC temperature too cold in office area (Medium, InProcess)
- Ticket #25: Fire alarm test scheduled - notify all staff (Critical, Returned)
- Ticket #42: Meeting room projector bulb replacement (Medium, Resolved)
- Ticket #45: Visitor parking validation system down (Low, Postponed)
- Ticket #47: Building security camera blind spot identified (High, InProcess)
- Ticket #49: Break room refrigerator not cooling (Low, Cancelled)

**Unassigned Pool:** See [Administration Category Breakdown](#administration-category-15-tickets-total) - Unassigned section (1 ticket)

**Breakdown of My Workload:**
- By Status: Open: 1, InProcess: 2, Resolved: 2, Cancelled: 1, Postponed: 1, Returned: 1
- By Priority: Low: 3, Medium: 3, High: 1, Critical: 1
- All are Administration category

---

### Employee (lisa.anderson@ironpack.pl)

**My Requests:** 31 tickets (created by me - heavy ticket creator)
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
- Ticket #27: Network switch showing errors on ports 12-24 (High, InProcess, IT)
- Ticket #29: Software license renewal approaching (Medium, Open, IT)
- Ticket #31: Mobile device management enrollment failing (High, Resolved, IT)
- Ticket #33: Inventory scanner batteries draining quickly (Medium, Resolved, Logistics)
- Ticket #35: Incorrect inventory count after annual audit (High, InProcess, Logistics)
- Ticket #37: Delivery route optimization request (Medium, InProcess, Logistics)
- Ticket #39: Refrigerated storage temperature fluctuation (High, Resolved, Logistics)
- Ticket #40: Return processing backlog (Medium, InProcess, Logistics)
- Ticket #42: Meeting room projector bulb replacement (Medium, Resolved, Administration)
- Ticket #44: Employee handbook update needed (Medium, InProcess, Administration)
- Ticket #46: Company newsletter deadline approaching (Medium, Open, Administration)
- Ticket #48: First aid kit expiration - multiple locations (Medium, Resolved, Administration)

**Note:** Employee role can ONLY see tickets they created. They cannot see other users' tickets.

**Breakdown of My Requests:**
- By Status: New: 3, Open: 3, InProcess: 10, Resolved: 13, Cancelled: 1, Returned: 1
- By Priority: Low: 3, Medium: 15, High: 9, Critical: 4
- By Category: IT: 12, Logistics: 10, Administration: 9

**Resolved Tickets:** #1, #2, #11, #15, #19, #21, #24, #31, #33, #39, #42, #48 (12 total)

---

### Employee (tom.harris@ironpack.pl)

**My Requests:** 18 tickets (created by me)
- Ticket #6: Printer not responding (Floor 2) (Low, New, IT)
- Ticket #8: Antivirus update failed on multiple computers (Medium, Open, IT)
- Ticket #12: Forklift maintenance required (Medium, InProcess, Logistics)
- Ticket #14: Loading dock light broken (Low, New, Logistics)
- Ticket #17: Delivery truck GPS malfunction (Medium, Postponed, Logistics)
- Ticket #20: Access card not working at main entrance (Medium, Open, Administration)
- Ticket #22: HVAC temperature too cold in office area (Medium, InProcess, Administration)
- Ticket #26: Secondary server backup verification failed (Critical, Resolved, IT)
- Ticket #28: Spam filter blocking legitimate emails (Medium, Resolved, IT)
- Ticket #30: Monitor flickering issue - multiple workstations (Low, InProcess, IT)
- Ticket #32: Cloud storage quota exceeded (Medium, InProcess, IT)
- Ticket #34: Shipping label printer jamming frequently (High, Open, Logistics)
- Ticket #36: Loading dock door motor failure (Critical, Resolved, Logistics)
- Ticket #38: Packing station ergonomics complaint (Low, Open, Logistics)
- Ticket #41: Courier service contract renewal (Medium, Open, Logistics)
- Ticket #43: Office cleaning schedule conflict (Low, Resolved, Administration)
- Ticket #45: Visitor parking validation system down (Low, Postponed, Administration)
- Ticket #47: Building security camera blind spot identified (High, InProcess, Administration)
- Ticket #49: Break room refrigerator not cooling (Low, Cancelled, Administration)

**Note:** Employee role can ONLY see tickets they created.

**Breakdown of My Requests:**
- By Status: New: 2, Open: 6, InProcess: 5, Resolved: 4, Cancelled: 1, Postponed: 2
- By Priority: Low: 8, Medium: 8, High: 1, Critical: 1
- By Category: IT: 6, Logistics: 6, Administration: 6

---

## Date Range Filters

**Today's Date:** January 13, 2026

### Last 7 Days (since January 6, 2026)
**Expected Count:** 17 tickets

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
- Ticket #29: Software license renewal approaching (6 days ago)
- Ticket #40: Return processing backlog (7 days ago)
- Ticket #42: Meeting room projector bulb replacement (9 days ago) - OUTSIDE 7 days
- Ticket #46: Company newsletter deadline approaching (5 days ago)

**Correction:** 16 tickets within last 7 days (excluding #42 which is 9 days ago)

### Last 30 Days (since December 14, 2025)
**Expected Count:** 39 tickets

Tickets with createdDaysAgo <= 30:
All tickets EXCEPT these 10:
- #1 (75 days), #2 (60 days), #9 (45 days), #11 (50 days), #15 (35 days), #19 (42 days), #21 (40 days), #24 (55 days), #43 (31 days), #49 (33 days)

**Actual count:** 39 tickets (49 - 10 = 39)

### Last 90 Days (since October 15, 2025)
**Expected Count:** 49 tickets (all tickets)

All 49 tickets are within the last 90 days.

**Date Range Note:** The seed data uses "createdDaysAgo" from seeding time, so actual dates will shift over time. For demo purposes on January 13, 2026, use the counts above as reference.

---

## Verification Checklist

### During Demo - What to Check:

#### Admin Login (admin@ironpack.pl)
- [ ] Dashboard shows: Total: 49, Pending: 14, In Progress: 14, Resolved: 16
- [ ] Dashboard by Status: New:5, Open:9, InProcess:14, Resolved:16, Cancelled:2, Postponed:2, Returned:1
- [ ] All Tickets view shows 49 tickets
- [ ] Filter by Category: IT=17, Logistics=17, Administration=15
- [ ] Filter by Status: New=5, Open=9, InProcess=14, Resolved=16, Cancelled=2, Postponed=2, Returned=1
- [ ] Filter by Priority: Low=11, Medium=21, High=12, Critical=5
- [ ] Active tickets (New+Open+InProcess): 28 total

#### TeamLeader Login (michael.johnson@ironpack.pl)
- [ ] My Workload shows 7 tickets (#2, #5, #9, #27, #29, #31)
- [ ] Team Tickets (IT) shows 17 tickets
- [ ] Can see unassigned IT tickets (#10)
- [ ] Dashboard accessible
- [ ] Dashboard shows IT category: Pending: 4, Active: 9
- [ ] Dashboard shows Active by Priority: Low:1, Medium:4, High:3, Critical:1

#### Support Login (david.smith@ironpack.pl)
- [ ] My Workload shows 9 tickets (#1, #3, #4, #7, #8, #26, #28, #30, #32)
- [ ] Unassigned Pool shows 1 IT ticket (#10)
- [ ] Can see all IT category tickets (17 total)
- [ ] Cannot see Logistics or Administration tickets

#### Employee Login (lisa.anderson@ironpack.pl)
- [ ] My Requests shows 31 tickets (all created by lisa.anderson)
- [ ] Resolved count: 12 tickets
- [ ] Cannot see tickets created by tom.harris
- [ ] Cannot see "All Tickets" or "Unassigned Pool" navigation items
- [ ] Can filter own tickets by Status/Priority/Category

#### Employee Login (tom.harris@ironpack.pl)
- [ ] My Requests shows 18 tickets (all created by tom.harris)
- [ ] Cannot see tickets created by lisa.anderson

---

## Special Test Cases

### Unassigned Tickets
**Total Unassigned:** 5 tickets
- IT: #6 (Low, New), #10 (High, New)
- Logistics: #14 (Low, New), #18 (Medium, New)
- Administration: #23 (Low, New)

**Note:** All unassigned tickets are in "New" status.

### Tickets with Attachments (12 tickets)
- Ticket #1: 2 attachments (1-PNG.png, 2-PDF.pdf)
- Ticket #3: 1 attachment (3-JPG.jpg)
- Ticket #4: 1 attachment (4.zip)
- Ticket #5: 1 attachment (5-PNG.png)
- Ticket #7: 2 attachments (1-PNG.png, 2-PDF.pdf)
- Ticket #12: 1 attachment (3-JPG.jpg)
- Ticket #15: 1 attachment (2-PDF.pdf)
- Ticket #26: 1 attachment (1-PNG.png)
- Ticket #31: 1 attachment (5-PNG.png)
- Ticket #35: 1 attachment (2-PDF.pdf)
- Ticket #39: 1 attachment (3-JPG.jpg)
- Ticket #47: 1 attachment (5-PNG.png)

### Tickets with Comments (43 tickets)
All tickets except: #6, #10, #14, #18, #23 (which have 0 comments)

### Tickets with Internal Comments (19 tickets)
- #1, #3, #5, #7, #9, #13, #16, #27, #31 (IT category)
- #35, #39 (Logistics category)
- #25, #44, #47 (Administration category)

---

**End of Validation Document**  
**Use this document during demo to verify UI displays against expected seed data.**
