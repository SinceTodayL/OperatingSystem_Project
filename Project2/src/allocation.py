import random
from dispatch import Dispatcher, Page

class Allocation:
    """Generate instruction sequence and manage execution"""
    
    def __init__(self, order_nums, request_order_nums):
        self.order_nums = order_nums
        self.request_order_nums = request_order_nums
        self.order_seq = []
        self.gen_seq()
        self.cur_index = 0

    def gen_seq(self):
        """Generate instruction execution sequence"""
        start = random.randint(0, self.order_nums-1)
        while len(self.order_seq) < self.request_order_nums:
            self.order_seq.append(start)
            if start < self.order_nums-1:
                self.order_seq.append(start+1)
            m1 = start
            if start >= 1:
                m1 = random.randint(0, max(0, start-1))
                self.order_seq.append(m1)
                if m1+1 < self.order_nums:
                    self.order_seq.append(m1+1)
            m2 = m1
            if m1 < self.order_nums-1:
                m2 = random.randint(m1+1, self.order_nums-1)
                self.order_seq.append(m2)
                if m2+1 < self.order_nums:
                    self.order_seq.append(m2+1)
            start = random.randint(0, m2)
        self.order_seq = self.order_seq[:self.request_order_nums]

    def cur_order(self):
        """Get current instruction"""
        return self.order_seq[self.cur_index]

    def next_order(self):
        """Get next instruction"""
        if self.cur_index < len(self.order_seq)-1:
            return self.order_seq[self.cur_index+1]
        else:
            return None

    def go_next(self):
        """Move to next instruction"""
        if self.cur_index < len(self.order_seq)-1:
            self.cur_index += 1

from enum import Enum

class State(Enum):
    NOT_STRAT = 0
    IN_PROGRESS = 1
    FINISHED = 2

class MemoryDispatch():
    """Main memory dispatch controller"""
    
    def __init__(self):
        self.state = State.NOT_STRAT
        self.dispatcher = Dispatcher(sum_page_number=4)
        self.allocation = Allocation(order_nums=320, request_order_nums=320)

    def start(self):
        """Start simulation"""
        self.state = State.IN_PROGRESS

    def next_order(self):
        """Get next order if simulation is in progress"""
        if self.allocation.cur_index >= self.allocation.request_order_nums:
            self.finish()
        if self.state == State.IN_PROGRESS:
            return self.allocation.cur_order

    def finish(self):
        """Finish simulation"""
        self.state = State.FINISHED

    def get_page(self):
        """Process current page request"""
        self.dispatcher.accept_request(self.allocation.cur_order() // 10)