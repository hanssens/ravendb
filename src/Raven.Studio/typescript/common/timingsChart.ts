/// <reference path="../../typings/tsd.d.ts"/>

import d3 = require("d3");

interface graphNode extends d3.layout.partition.Node {
    name: string;
} 

class timingsChart {
    
    private totalSize = 0;
    
    constructor(private selector: string) {
    }
    
    draw(data: Raven.Client.Documents.Queries.Timings.QueryTimings) {
        d3.select(this.selector).select("svg").remove();
        
        const container = $(this.selector);
        
        const topPadding = 50;
        
        const width = container.width();
        const height = container.height();
        const radius = Math.min(width, height) - topPadding;
        
        const colors = { //TODO: update me!
            "Storage": "#5687d1",
            "Function": "#7b615c",
            "Lucene": "#de783b",
            "Projection": "#6ab975",
            "Staleness": "#a173d1",
            "Query": "#bbbbbb"
        } as dictionary<string>;
        
        const vis = d3.select(this.selector)
            .append("svg:svg")
            .attr("width", width)
            .attr("height", height)
            .append("svg:g")
            .attr("id", "container")
            .attr("transform", "translate(" + width / 2 + "," + height + ")");
        
        const partition = d3.layout.partition<graphNode>()
            .size([Math.PI, radius * radius]);
        
        const arc = d3.svg.arc<graphNode>()
            .startAngle(d => -0.5 * Math.PI + d.x)
            .endAngle(d => -0.5 * Math.PI + d.x + d.dx)
            .innerRadius(d => Math.sqrt(d.y))
            .outerRadius(d => Math.sqrt(d.y + d.dy));
        
        const json = this.convertHierarchy("root", data);
        
        // Bounding circle underneath the sunburst, to make it easier to detect
        // when the mouse leaves the parent g.
        vis
            .append("svg:circle")
            .attr("r", radius)
            .style("opacity", 0);

        // For efficiency, filter nodes to keep only those large enough to see.
        const nodes = partition
            .nodes(json)
            .filter(d => d.dx > 0.005);

        const levelName = vis
            .append("svg:text")
            .attr("class", "levelName")
            .attr("y", -50)
            .text("Total");

        const levelDuration = vis.append("svg:text")
            .attr("class", "duration")
            .attr("y", -8);
        
        const path = vis
            .data([json])
            .selectAll("path")
            .data(nodes)
            .enter()
            .append("svg:path")
            .attr("display", d => d.depth ? null : "none")
            .attr("d", arc)
            .attr("fill-rule", "evenodd")
            .style("fill", d => colors[d.name] ||  "#554433") //TODO: update callback color
            .style("opacity", 1)
            .on("mouseover", d => this.mouseover(vis, d, levelName, levelDuration));

        // Add the mouseleave handler to the bounding circle.
        vis
            .on("mouseleave", () => this.mouseleave(vis, levelName, levelDuration));
        
        // Get total size of the tree = value of root node from partition.
        this.totalSize = (path.node() as any).__data__.value;
        
        levelDuration
            .text(this.totalSize.toLocaleString() + " ms");
    }
    
    private mouseover(vis: d3.Selection<any>, d: graphNode, levelName: d3.Selection<any>, levelDuration: d3.Selection<any>) {
        // Fade all but the current sequence, and show it in the breadcrumb trail.
        const percentage = (100 * d.value / this.totalSize);
        let percentageString = percentage.toPrecision(3) + "%";
        
        if (percentage < 0.1) {
            percentageString = "< 0.1%";
        }
        
        levelName
            .text(d.name);
        
        levelDuration
            .text(d.value.toLocaleString() + " ms");
        
        const sequenceArray = this.getAncestors(d);
        
        // Fade all the segments.
        vis
            .selectAll("path")
            .style("opacity", 0.3);
        
        vis
            .selectAll("path")
            .filter(n => sequenceArray.indexOf(n) >= 0)
            .style("opacity", 1);
    }
    
    private mouseleave(vis: d3.Selection<any>, levelName: d3.Selection<any>, levelDuration: d3.Selection<any>) {
        // Restore everything to full opacity when moving off the visualization.
        
        levelName
            .text("Total");
        
        levelDuration
            .text(this.totalSize.toLocaleString() + " ms");
        
        const self = this;
        
        vis.selectAll("path").on("mouseover", null);

        // Transition each segment to full opacity and then reactivate it.
        vis
            .selectAll("path")
            .transition()
            .duration(200)
            .style("opacity", 1)
            .each("end", function() {
                d3.select(this).on("mouseover", d => self.mouseover(vis, d, levelName, levelDuration));
            });
    }
    
    private convertHierarchy(name: string, data: Raven.Client.Documents.Queries.Timings.QueryTimings): graphNode {
        return {
            name: name,
            value: data.DurationInMs,
            children: _.map(data.Timings, (value, key) => this.convertHierarchy(key, value)) as any
        }
    }
    
    private getAncestors(node: graphNode) {
        // Given a node in a partition layout, return an array of all of its ancestor
        // nodes, highest first, but excluding the root.
        
        let path = [] as Array<graphNode>;
        let current = node;
        while (current.parent) {
            path.unshift(current);
            current = current.parent as graphNode;
        }
        return path;
    } 
}

export = timingsChart;