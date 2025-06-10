FLOOR_NUM = 20
ELEVATOR_NUM = 5


class Dispatcher:
    """
    Manage positions, requests, states, alerts and door flags for all elevators.
    """
    def __init__(self):
        # per-elevator current floor
        self.floors = [1] * ELEVATOR_NUM
        # per-elevator sets of internal targets
        self.targets = [set() for _ in range(ELEVATOR_NUM)]
        # global set of pending external call floors
        self.external_requests = set()
        # per-elevator movement state: -1=down, 0=idle, 1=up
        self.states = [0] * ELEVATOR_NUM
        # per-elevator alert flag
        self.alerts = [False] * ELEVATOR_NUM
        # per-elevator door-open flag
        self.opens = [False] * ELEVATOR_NUM

    def update_elevator(self, idx: int):
        """
        Step simulation for elevator idx (0-based):
        move, open/close doors, clear requests, recalc state.
        """
        if not self.alerts[idx]:
            # move one floor if needed
            if self.states[idx] == -1 and self.floors[idx] > 1:
                self.floors[idx] -= 1
            elif self.states[idx] == 1 and self.floors[idx] < FLOOR_NUM:
                self.floors[idx] += 1

            # open door if at a requested floor
            if self.floors[idx] in self.targets[idx] or \
               self.floors[idx] in self.external_requests:
                self.opens[idx] = True
                self.targets[idx].discard(self.floors[idx])
                self.external_requests.discard(self.floors[idx])
            else:
                self.opens[idx] = False

            # recalculate movement state
            self._update_state(idx)

    def _update_state(self, idx: int):
        """
        Decide new movement direction based on remaining internal targets.
        """
        if self.alerts[idx] or not self.targets[idx]:
            self.states[idx] = 0
            return

        current = self.floors[idx]
        highest = max(self.targets[idx])
        lowest = min(self.targets[idx])

        if self.states[idx] == 0:
            # idle: choose nearest target direction
            if abs(highest - current) >= abs(lowest - current):
                self.states[idx] = -1 if lowest < current else 1
            else:
                self.states[idx] = 1 if highest > current else -1
        elif self.states[idx] == 1 and highest < current:
            self.states[idx] = -1
        elif self.states[idx] == -1 and lowest > current:
            self.states[idx] = 1

    def assign_internal(self, elevator_id: int, floor: int):
        """
        Add an internal request for a specific elevator.
        """
        idx = elevator_id - 1
        if floor != self.floors[idx]:
            self.targets[idx].add(floor)
        return elevator_id

    def assign_external(self, floor: int, direction: str = None) -> int:
        """
        Handle an external up/down call at `floor`:
        1) Prefer an elevator already moving in the same `direction` that will pass `floor`.
        2) Otherwise, choose the nearest idle elevator.
        3) Otherwise, choose the nearest any-direction elevator.
        Returns the assigned elevator_id (1-based), or -1 if none available.
        """
        best_idx = -1
        best_score = float('inf')

        # Phase 1: elevators moving in same direction that will pass the floor
        for idx in range(ELEVATOR_NUM):
            if self.alerts[idx]:
                continue

            # Only consider those already moving in requested direction
            if direction == 'up' and self.states[idx] == 1 and self.floors[idx] <= floor:
                score = floor - self.floors[idx]
            elif direction == 'down' and self.states[idx] == -1 and self.floors[idx] >= floor:
                score = self.floors[idx] - floor
            else:
                continue

            if score < best_score:
                best_score, best_idx = score, idx

        # Phase 2: no moving elevator qualifies → look for idle elevator
        if best_idx == -1:
            idle_score = float('inf')
            for idx in range(ELEVATOR_NUM):
                if self.alerts[idx] or self.states[idx] != 0:
                    continue
                score = abs(self.floors[idx] - floor)
                if score < idle_score:
                    idle_score, best_idx = score, idx

        # Phase 3: still none → pick nearest any-direction elevator
        if best_idx == -1:
            any_score = float('inf')
            for idx in range(ELEVATOR_NUM):
                if self.alerts[idx]:
                    continue
                score = abs(self.floors[idx] - floor)
                if score < any_score:
                    any_score, best_idx = score, idx

        if best_idx >= 0:
            # Record the request for the chosen elevator
            self.targets[best_idx].add(floor)
            self.external_requests.add(floor)
            # If already at the same floor, open the door immediately
            if self.floors[best_idx] == floor:
                self.opens[best_idx] = True
            return best_idx + 1

        # No elevator available
        return -1

    def toggle_alert(self, elevator_id: int) -> bool:
        """
        Toggle alert for one elevator.
        If entering alert, reassign its external calls.
        Returns new alert status.
        """
        idx = elevator_id - 1
        self.alerts[idx] = not self.alerts[idx]
        if self.alerts[idx]:
            # remove this elevator's pending external targets
            pending = {f for f in self.targets[idx] if f in self.external_requests}
            self.targets[idx] -= pending
            # reassign all external requests
            for floor in list(self.external_requests):
                self.assign_external(floor)
        return self.alerts[idx]

